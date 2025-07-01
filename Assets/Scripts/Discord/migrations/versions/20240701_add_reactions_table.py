"""add reactions table

Revision ID: 20240701_add_reactions_table
Revises: 20240630_add_tools_table
Create Date: 2024-07-01

"""
from alembic import op
import sqlalchemy as sa

revision = '20240701_add_reactions_table'
down_revision = '20240630_add_tools_table'
branch_labels = None
depends_on = None

def upgrade():
    op.create_table(
        'reactions',
        sa.Column('id', sa.Integer(), primary_key=True, autoincrement=True),
        sa.Column('elements', sa.String(), nullable=False),
        sa.Column('conditions', sa.String(), nullable=False),
        sa.Column('tool', sa.String(), nullable=False),
        sa.Column('compound', sa.String(), nullable=False),
        sa.Column('style', sa.Integer()),
        sa.Column('fun_fact', sa.String()),
        sa.UniqueConstraint('elements', 'conditions', 'tool')
    )

def downgrade():
    op.drop_table('reactions')
