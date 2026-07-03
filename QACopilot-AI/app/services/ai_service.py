from app.config import get_settings
from app.services.claude_service import ClaudeService
from app.services.openai_service import OpenAIService
from app.prompts.system_prompts import (
    TESTCASE_GENERATION_PROMPT,
    TESTPLAN_ANALYSIS_PROMPT,
    CHAT_QA_PROMPT,
    REPORT_GENERATION_PROMPT,
    BASE_SYSTEM_PROMPT
)
from app.models.responses import (
    TestCaseGenerationResponse,
    TestPlanAnalysisResponse,
    ChatResponse,
    ReportResponse
)
import json
import re
import logging

settings = get_settings()
logger = logging.getLogger(__name__)


def get_ai_client():
    if settings.ai_provider.lower() == "claude":
        return ClaudeService(), f"claude/{settings.claude_model}"
    return OpenAIService(), f"openai/{settings.openai_model}"


async def generate_test_cases(
    document_content: str,
    project_name: str = "",
    additional_context: str = ""
) -> TestCaseGenerationResponse:
    client, model_name = get_ai_client()
    context = f"Proyecto: {project_name}\n" if project_name else ""
    if additional_context:
        context += f"Contexto adicional: {additional_context}\n"
    prompt = f"{TESTCASE_GENERATION_PROMPT}\n\n{context}{document_content}"
    content, tokens = await client.generate(prompt)
    tc_count = len(re.findall(r'TC-[0-9]+', content))
    if tc_count == 0:
        tc_count = len(re.findall(r'Caso de Prueba [0-9]+', content, re.IGNORECASE))
    if tc_count == 0:
        tc_count = content.lower().count('### caso de prueba')
    if tc_count == 0:
        tc_count = content.lower().count('**id:**')
    confidence = 0.0
    if tc_count > 0:
        confidence = min(0.95, 0.70 + (tc_count * 0.02))
        if 'precondicion' in content.lower():
            confidence = min(0.95, confidence + 0.05)
        if 'resultado esperado' in content.lower():
            confidence = min(0.95, confidence + 0.05)
        if 'istqb' in content.lower():
            confidence = min(0.95, confidence + 0.03)
    return TestCaseGenerationResponse(
        content=content,
        total_test_cases=max(tc_count, 1),
        confidence_score=round(confidence, 2),
        model_used=model_name,
        tokens_used=tokens
    )


async def analyze_test_plan(
    plan_content: str,
    project_name: str = ""
) -> TestPlanAnalysisResponse:
    client, model_name = get_ai_client()
    context = f"Proyecto: {project_name}\n" if project_name else ""
    prompt = f"{TESTPLAN_ANALYSIS_PROMPT}\n\n{context}{plan_content}"
    content, tokens = await client.generate(prompt)
    is_viable = any(word in content.lower() for word in ["viable", "factible", "aprobado", "cumple"])
    not_viable = any(word in content.lower() for word in ["no viable", "no factible", "rechazado"])
    if not_viable:
        is_viable = False
    return TestPlanAnalysisResponse(
        is_viable=is_viable,
        viability_reason=content[:500],
        istqb_compliance_notes=_extract_section(content, "ISTQB"),
        iso29119_compliance_notes=_extract_section(content, "ISO"),
        estimated_time_json=_extract_time_estimation(content),
        ai_analysis_result=content,
        confidence_score=0.88,
        model_used=model_name
    )


async def chat_with_qa_assistant(
    message: str,
    session_history: list[dict] = []
) -> ChatResponse:
    client, model_name = get_ai_client()
    try:
        if session_history and len(session_history) > 0:
            messages = [{"role": "system", "content": BASE_SYSTEM_PROMPT}]
            recent_history = session_history[-10:] if len(session_history) > 10 else session_history
            for msg in recent_history:
                if msg.get("role") in ["user", "assistant"]:
                    messages.append({"role": msg["role"], "content": msg["content"]})
            messages.append({"role": "user", "content": message})
            content, tokens = await client.generate_with_history(messages)
        else:
            prompt = f"{CHAT_QA_PROMPT}{message}"
            content, tokens = await client.generate(prompt)
        return ChatResponse(response=content, model_used=model_name, tokens_used=tokens)
    except Exception as e:
        logger.error(f"Error in chat: {e}")
        prompt = f"{CHAT_QA_PROMPT}{message}"
        content, tokens = await client.generate(prompt)
        return ChatResponse(response=content, model_used=model_name, tokens_used=tokens)


async def generate_report(
    structure: str,
    instructions: str,
    context: str = ""
) -> ReportResponse:
    client, model_name = get_ai_client()
    prompt = REPORT_GENERATION_PROMPT.format(
        structure=structure,
        instructions=instructions,
        context=context or "No se proporciono contexto adicional."
    )
    content, tokens = await client.generate(prompt)
    return ReportResponse(content=content, model_used=model_name, tokens_used=tokens)


def _extract_section(content: str, keyword: str) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if keyword.upper() in line.upper():
            capturing = True
        elif capturing and line.strip().startswith('#'):
            break
        if capturing:
            result.append(line)
    return '\n'.join(result[:10]) if result else f"Ver analisis completo para detalles de {keyword}."


def _extract_time_estimation(content: str) -> str:
    time_data = {
        "planificacion": "2-3 dias",
        "diseno_casos": "3-5 dias",
        "preparacion_entorno": "1-2 dias",
        "ejecucion": "5-8 dias",
        "reporte_cierre": "1-2 dias",
        "total_optimista": "12 dias",
        "total_probable": "15 dias",
        "total_pesimista": "20 dias"
    }
    return json.dumps(time_data, ensure_ascii=False)
