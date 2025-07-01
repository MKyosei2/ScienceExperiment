"""add elements table

Revision ID: 20240630_add_elements_table
Revises: 
Create Date: 2024-06-30

"""
from alembic import op
import sqlalchemy as sa

revision = '20240630_add_elements_table'
down_revision = None
branch_labels = None
depends_on = None

def upgrade():
    op.create_table(
        'elements',
        sa.Column('symbol', sa.String(), primary_key=True),
        sa.Column('name', sa.String(), nullable=False),
        sa.Column('category', sa.String()),
        sa.Column('atomic_number', sa.Integer()),
        sa.Column('phase', sa.String()),
        sa.Column('description', sa.String()),
    )

def downgrade():
    op.drop_table('elements')
