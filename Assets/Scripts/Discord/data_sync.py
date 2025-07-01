import os
import json
import sqlite3
import aiohttp
import discord
from discord.ext import commands
from dotenv import load_dotenv

load_dotenv()
TOKEN = os.getenv("DISCORD_TOKEN")
ELEMENT_CHANNEL_ID = int(os.getenv("ELEMENT_CHANNEL_ID"))
TOOL_CHANNEL_ID = int(os.getenv("TOOL_CHANNEL_ID"))
REACTION_CHANNEL_ID = int(os.getenv("REACTION_CHANNEL_ID"))

DB_FILE = "chem_data.db"

intents = discord.Intents.default()
intents.messages = True
bot = commands.Bot(command_prefix="!", intents=intents)

def init_db():
    conn = sqlite3.connect(DB_FILE)
    c = conn.cursor()
    c.execute("""
        CREATE TABLE IF NOT EXISTS elements (
            symbol TEXT PRIMARY KEY,
            name TEXT,
            category TEXT,
            atomic_number INTEGER,
            phase TEXT,
            description TEXT
        )
    """)
    c.execute("""
        CREATE TABLE IF NOT EXISTS tools (
            tool_id TEXT PRIMARY KEY,
            name TEXT,
            reusable BOOLEAN,
            requires_power BOOLEAN,
            description TEXT
        )
    """)
    c.execute("""
        CREATE TABLE IF NOT EXISTS reactions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            elements TEXT,
            conditions TEXT,
            tool TEXT,
            compound TEXT,
            style INTEGER,
            fun_fact TEXT,
            UNIQUE(elements, conditions, tool)
        )
    """)
    conn.commit()
    conn.close()

async def fetch_json_from_channel(channel_id):
    channel = bot.get_channel(channel_id)
    pins = await channel.pins()
    for msg in pins:
        for attachment in msg.attachments:
            if attachment.filename.endswith(".json"):
                async with aiohttp.ClientSession() as session:
                    async with session.get(attachment.url) as resp:
                        data = await resp.text()
                        return json.loads(data)
    return None

def save_elements(data):
    conn = sqlite3.connect(DB_FILE)
    c = conn.cursor()
    for e in data:
        c.execute("""
            INSERT OR REPLACE INTO elements (symbol, name, category, atomic_number, phase, description)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (e["symbol"], e["name"], e["category"], e["atomic_number"], e["phase"], e.get("description", "")))
    conn.commit()
    conn.close()

def save_tools(data):
    conn = sqlite3.connect(DB_FILE)
    c = conn.cursor()
    for t in data:
        c.execute("""
            INSERT OR REPLACE INTO tools (tool_id, name, reusable, requires_power, description)
            VALUES (?, ?, ?, ?, ?)
        """, (t["tool_id"], t["name"], t["reusable"], t["requires_power"], t.get("description", "")))
    conn.commit()
    conn.close()

def save_reactions(data):
    conn = sqlite3.connect(DB_FILE)
    c = conn.cursor()
    for r in data:
        c.execute("""
            INSERT OR REPLACE INTO reactions (elements, conditions, tool, compound, style, fun_fact)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (r["elements"], r["conditions"], r["tool"], r["compound"], r["style"], r.get("fun_fact", "")))
    conn.commit()
    conn.close()

@bot.event
async def on_ready():
    print(f"[Bot] ログイン成功: {bot.user}")

    print("📦 Fetching JSON files from Discord...")
    e_data = await fetch_json_from_channel(ELEMENT_CHANNEL_ID)
    t_data = await fetch_json_from_channel(TOOL_CHANNEL_ID)
    r_data = await fetch_json_from_channel(REACTION_CHANNEL_ID)

    if e_data:
        save_elements(e_data)
        print("✅ elements.json saved.")
    if t_data:
        save_tools(t_data)
        print("✅ tools.json saved.")
    if r_data:
        save_reactions(r_data)
        print("✅ reactions.json saved.")

    await bot.close()

if __name__ == "__main__":
    init_db()
    bot.run(TOKEN)
