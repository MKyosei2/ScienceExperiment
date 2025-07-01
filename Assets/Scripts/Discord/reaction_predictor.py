import sqlite3
import openai
import os

openai.api_key = os.getenv("OPENAI_API_KEY")  # .envから読み込み

def predict_compound(elements, tool, conditions):
    key_elems = ",".join(sorted(elements))
    key_cond = conditions
    key_tool = tool

    conn = sqlite3.connect("chem_data.db")
    c = conn.cursor()

    # Step 1: 辞書検索
    c.execute("""
        SELECT compound, style, fun_fact FROM reactions
        WHERE elements=? AND conditions=? AND tool=?
    """, (key_elems, key_cond, key_tool))
    row = c.fetchone()
    if row:
        return {
            "compound": row[0],
            "style": row[1],
            "funFact": row[2],
            "source": "dictionary"
        }

    # Step 2: Few-shot prompt (簡易版)
    c.execute("SELECT elements, conditions, tool, compound FROM reactions ORDER BY RANDOM() LIMIT 3")
    examples = c.fetchall()

    shots = "\n".join([f"{e} | {c} | {t} -> {r}" for e, c, t, r in examples])
    prompt = f"""
You are an AI chemistry assistant. Based on known reactions:

{shots}

Now predict the result of:
{key_elems} | {key_cond} | {key_tool}
Return compound name only.
"""

    response = openai.ChatCompletion.create(
        model="gpt-4",
        messages=[
            {"role": "system", "content": "You are a chemical reaction expert."},
            {"role": "user", "content": prompt}
        ]
    )

    result = response.choices[0].message.content.strip()
    return {
        "compound": result,
        "style": 0,
        "funFact": "(AI predicted)",
        "source": "ai"
    }
