"""add tools table

Revision ID: 20240630_add_tools_table
Revises: 20240630_add_elements_table
Create Date: 2024-06-30

"""
from alembic import op
import sqlalchemy as sa

revision = '20240630_add_tools_table'
down_revision = '20240630_add_elements_table'
branch_labels = None
depends_on = None

def upgrade():
    op.create_table(
        'tools',
        sa.Column('tool_id', sa.String(), primary_key=True),
        sa.Column('name', sa.String(), nullable=False),
        sa.Column('reusable', sa.Boolean()),
        sa.Column('requires_power', sa.Boolean()),
        sa.Column('description', sa.String()),
    )

def downgrade():
    op.drop_table('tools')
