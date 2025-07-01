from logging.config import fileConfig
from sqlalchemy import engine_from_config
from sqlalchemy import pool
from alembic import context
import os

# この config オブジェクトは .ini ファイルの値を含みます
config = context.config

# Logging の設定
if config.config_file_name is not None:
    fileConfig(config.config_file_name)

# ここで metadata を指定しない（自動生成しない構成）
target_metadata = None

# SQLite ファイル名指定（例: chem_data.db）
DB_URL = os.getenv("DB_URL", "sqlite:///chem_data.db")
config.set_main_option("sqlalchemy.url", DB_URL)

def run_migrations_offline():
    context.configure(
        url=DB_URL,
        literal_binds=True,
        dialect_opts={"paramstyle": "named"},
    )
    with context.begin_transaction():
        context.run_migrations()

def run_migrations_online():
    connectable = engine_from_config(
        config.get_section(config.config_ini_section),
        prefix="sqlalchemy.",
        poolclass=pool.NullPool,
    )
    with connectable.connect() as connection:
        context.configure(
            connection=connection,
            target_metadata=target_metadata
        )
        with context.begin_transaction():
            context.run_migrations()

if context.is_offline_mode():
    run_migrations_offline()
else:
    run_migrations_online()
