from sqlalchemy import Column, Integer, String, DateTime, Text
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

# SQLAlchemy의 기본 베이스 클래스 생성
Base = declarative_base()

class DonorRecord(Base):
    """
    기부자 정보를 저장하는 메인 테이블 모델
    """
    __tablename__ = "donors"

    # 기본키 및 식별 정보
    id = Column(Integer, primary_key=True, index=True)
    
    # 기부자 상세 정보
    name = Column(String(50), nullable=False, index=True)  # 기부자 성함 (검색 최적화)
    amount = Column(Integer, nullable=False, default=0)   # 기부 금액
    grade = Column(String(20), nullable=False)            # VVIP, GOLD, SILVER 등 등급
    message = Column(Text, nullable=True)                 # 기부 한마디 (긴 문장 대비 Text 타입)
    
    # 메타데이터
    created_at = Column(DateTime, default=datetime.utcnow) # 등록 일시 (UTC 기준)

    def __repr__(self):
        return f"<Donor(name={self.name}, amount={self.amount}, grade={self.grade})>"

class SystemConfig(Base):
    """
    (확장용) 디스플레이 설정 등을 DB에서 관리하고 싶을 경우 사용
    예: 현재 디스플레이 모드, 배경색 설정 등
    """
    __tablename__ = "system_config"
    
    id = Column(Integer, primary_key=True)
    config_key = Column(String(50), unique=True)
    config_value = Column(String(255))