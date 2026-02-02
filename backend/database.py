from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

# SQLite 사용 (상용 전환 시 PostgreSQL 주소로 변경 가능)
SQLALCHEMY_DATABASE_URL = "sqlite:///./donors.db"

engine = create_engine(
    SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def init_db():
    import models
    models.Base.metadata.create_all(bind=engine)