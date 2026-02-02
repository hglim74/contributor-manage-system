import io
import json
import logging
import asyncio
from datetime import datetime
from typing import List

import pandas as pd
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from sqlalchemy import Column, Integer, String, DateTime, create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session

# 1. 로깅 및 기본 설정
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("DonorManager")

# 2. 데이터베이스 설정 (SQLite)
SQLALCHEMY_DATABASE_URL = "sqlite:///./donors.db"
engine = create_engine(SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# 3. DB 모델 및 스키마 정의
class DonorRecord(Base):
    __tablename__ = "donors"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, index=True)
    amount = Column(Integer)
    grade = Column(String)  # VVIP, GOLD, SILVER
    message = Column(String)
    created_at = Column(DateTime, default=datetime.utcnow)

Base.metadata.create_all(bind=engine)

# Pydantic 모델 (API 데이터 검증용)
class DonorCreate(BaseModel):
    name: str
    amount: int
    grade: str
    message: str = ""

# 4. WebSocket 연결 관리 클래스
class ConnectionManager:
    def __init__(self):
        self.active_connections: List[WebSocket] = []

    async def connect(self, websocket: WebSocket):
        await websocket.accept()
        self.active_connections.append(websocket)
        logger.info(f"Unity Client Connected. Total: {len(self.active_connections)}")

    def disconnect(self, websocket: WebSocket):
        if websocket in self.active_connections:
            self.active_connections.remove(websocket)
            logger.info("Unity Client Disconnected.")

    async def broadcast(self, message: dict):
        json_str = json.dumps(message, ensure_ascii=False)
        for connection in self.active_connections:
            try:
                await connection.send_text(json_str)
            except Exception as e:
                logger.error(f"Broadcast error: {e}")

manager = ConnectionManager()

from fastapi.staticfiles import StaticFiles

# 5. FastAPI 앱 초기화 및 미들웨어
app = FastAPI(title="Donor Display Management System")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # 상용 시 특정 도메인으로 제한 권장
    allow_methods=["*"],
    allow_headers=["*"],
)

# 정적 파일 서빙 (Admin 페이지)
app.mount("/static", StaticFiles(directory="static", html=True), name="static")

# DB 세션 의존성
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# 6. API 엔드포인트 구현

@app.websocket("/ws/display")
async def websocket_endpoint(websocket: WebSocket):
    """Unity 클라이언트가 연결할 엔드포인트"""
    await manager.connect(websocket)
    try:
        while True:
            # 클라이언트로부터 오는 메시지는 없어도 되지만 연결 유지를 위해 대기
            await websocket.receive_text()
    except WebSocketDisconnect:
        manager.disconnect(websocket)

@app.post("/api/v1/donors")
async def create_donor(donor: DonorCreate, db: Session = Depends(get_db)):
    """단일 기부자 등록 API"""
    # DB 저장
    db_donor = DonorRecord(**donor.dict())
    db.add(db_donor)
    db.commit()
    db.refresh(db_donor)

    # Unity로 실시간 브로드캐스트
    payload = {
        "type": "NEW_DONOR",
        "payload": {
            "name": db_donor.name,
            "amount": db_donor.amount,
            "grade": db_donor.grade,
            "message": db_donor.message
        }
    }
    await manager.broadcast(payload)
    return {"status": "success", "id": db_donor.id}

@app.put("/api/v1/donors/{donor_id}")
async def update_donor(donor_id: int, donor: DonorCreate, db: Session = Depends(get_db)):
    """기부자 정보 수정 API"""
    db_donor = db.query(DonorRecord).filter(DonorRecord.id == donor_id).first()
    if not db_donor:
        raise HTTPException(status_code=404, detail="Donor not found")
    
    db_donor.name = donor.name
    db_donor.amount = donor.amount
    db_donor.grade = donor.grade
    db_donor.message = donor.message
    
    db.commit()
    db.refresh(db_donor)
    return {"status": "success", "id": db_donor.id}

@app.delete("/api/v1/donors/{donor_id}")
async def delete_donor(donor_id: int, db: Session = Depends(get_db)):
    """기부자 삭제 API"""
    db_donor = db.query(DonorRecord).filter(DonorRecord.id == donor_id).first()
    if not db_donor:
        raise HTTPException(status_code=404, detail="Donor not found")
    
    db.delete(db_donor)
    db.commit()
    return {"status": "success", "id": donor_id}

@app.post("/api/v1/donors/bulk")
async def bulk_upload_donors(file: UploadFile = File(...), db: Session = Depends(get_db)):
    """CSV 파일을 통한 대량 등록 및 실시간 송출 API"""
    if not file.filename.endswith('.csv'):
        raise HTTPException(status_code=400, detail="CSV 파일만 업로드 가능합니다.")

    content = await file.read()
    try:
        df = pd.read_csv(io.BytesIO(content))
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"CSV 파싱 에러: {str(e)}")

    processed_count = 0
    for _, row in df.iterrows():
        # 데이터 정제 및 DB 저장
        donor_data = {
            "name": str(row['name']),
            "amount": int(row['amount']),
            "grade": str(row['grade']).upper(),
            "message": str(row.get('message', ''))
        }
        
        db_donor = DonorRecord(**donor_data)
        db.add(db_donor)
        db.commit()

        # Unity 송출 (연출을 위해 1.5초 간격 유지)
        payload = {"type": "NEW_DONOR", "payload": donor_data}
        await manager.broadcast(payload)
        await asyncio.sleep(1.5)
        
        processed_count += 1

    return {"status": "success", "total_processed": processed_count}

@app.get("/api/v1/donors")
async def get_donors(db: Session = Depends(get_db)):
    """전체 기부자 명단 조회"""
    return db.query(DonorRecord).all()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)