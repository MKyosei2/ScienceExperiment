import discord
from discord.ext import commands
import aiohttp
import os
from dotenv import load_dotenv

load_dotenv()
TOKEN = os.getenv("DISCORD_TOKEN")
PREDICT_API_URL = os.getenv("PREDICT_API_URL")

intents = discord.Intents.default()
intents.message_content = True
bot = commands.Bot(command_prefix="!", intents=intents)

@bot.event
async def on_ready():
    print(f"✅ Discord Bot 起動: {bot.user}")

@bot.command()
async def predict(ctx, *args):
    """
    使用例: !predict Na,Cl Burner ZeroG,North
    """
    if len(args) != 3:
        await ctx.send("使用方法: `!predict 元素(カンマ区切り) 器具 環境条件(カンマ区切り)`\n例: `!predict Na,Cl Burner ZeroG,North`")
        return

    elements = [e.strip() for e in args[0].split(",")]
    tool = args[1].strip()
    conditions = args[2].strip()

    # APIにPOST送信
    payload = {
        "elements": elements,
        "tool": tool,
        "conditions": conditions
    }

    async with aiohttp.ClientSession() as session:
        async with session.post(PREDICT_API_URL, json=payload) as resp:
            if resp.status != 200:
                await ctx.send("⚠️ API呼び出しに失敗しました")
                return
            result = await resp.json()

    # DiscordにEmbed表示
    embed = discord.Embed(
        title=f"🧪 実験結果：{', '.join(elements)} × {tool} @ {conditions}",
        description=f"🔬 生成化合物：**{result['compound']}**\n🎨 スタイル番号：**{result['style']}**\n💬 解説：{result['funFact']}",
        color=0x00ffcc
    )
    embed.set_footer(text=f"情報元: {result.get('source', 'AI')}")
    await ctx.send(embed=embed)
