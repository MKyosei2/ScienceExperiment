from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel
import uvicorn
from reaction_predictor import predict_compound
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()

# 最新キャッシュ
latest_result = {
    "compound": "None",
    "style": 0,
    "funFact": "None"
}

# UnityからCORS許可（重要）
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"]
)

class PredictRequest(BaseModel):
    elements: list[str]
    tool: str
    conditions: str

@app.post("/api/predict")
async def predict(request: PredictRequest):
    global latest_result
    result = predict_compound(request.elements, request.tool, request.conditions)
    latest_result = result
    return JSONResponse(result)

@app.get("/api/last")
async def get_last():
    return JSONResponse(latest_result)
