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

    # Detectar viabilidad
    content_lower = content.lower()
    not_viable = any(w in content_lower for w in ["no viable", "no factible", "rechazado", "viable: false", "viable: no"])
    is_viable = not not_viable and any(w in content_lower for w in ["viable", "factible", "aprobado", "listo para pruebas"])

      # Limpiar líneas con Viable: true/false
    display_content = content
    lines = display_content.split('\n')
    cleaned_lines = [
        line for line in lines
        if not re.search(r'(?i)viable\s*:\s*(true|false)', line)
    ]
    display_content = '\n'.join(cleaned_lines)

    return TestPlanAnalysisResponse(
        is_viable=is_viable,
        viability_reason=_extract_section_numbered(display_content, "2", "RAZON") or display_content[:500],
        istqb_compliance_notes=_extract_istqb(display_content),
        iso29119_compliance_notes=_extract_iso(display_content),
        estimated_time_json=_extract_time_estimation(display_content),
        ai_analysis_result=display_content,
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
        logger.error("Error in chat: %s", str(e))
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


def _extract_istqb(content: str) -> str:
    """Extrae sección ISTQB del análisis."""
    patterns = ["ISTQB", "istqb", "4. ASPECTOS FUERTES", "ASPECTOS FUERTES"]
    for pattern in patterns:
        result = _extract_by_keyword(content, pattern, stop_patterns=["5.", "6.", "ASPECTOS A MEJORAR"])
        if result and len(result) > 50:
            return result
    return _extract_section(content, "ISTQB")


def _extract_iso(content: str) -> str:
    """Extrae sección ISO 29119 del análisis."""
    patterns = ["3. CUMPLIMIENTO ISO", "CUMPLIMIENTO ISO 29119", "ISO 29119", "ISO/IEC"]
    for pattern in patterns:
        result = _extract_by_keyword(content, pattern, stop_patterns=["4.", "ASPECTOS FUERTES"])
        if result and len(result) > 50:
            return result
    return _extract_section(content, "ISO")


def _extract_by_keyword(content: str, keyword: str, stop_patterns: list = None) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if keyword.upper() in line.upper() and not capturing:
            capturing = True
        elif capturing:
            if stop_patterns:
                should_stop = any(line.strip().startswith(s) for s in stop_patterns)
                if should_stop and result:
                    break
        if capturing:
            result.append(line)
        if capturing and len(result) > 15:
            break
    return '\n'.join(result).strip() if result else ""


def _extract_section_numbered(content: str, number: str, keyword: str) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if (f"{number}." in line or keyword.upper() in line.upper()) and not capturing:
            capturing = True
        elif capturing and re.match(r'^\d+\.', line.strip()) and result:
            break
        if capturing:
            result.append(line)
    return '\n'.join(result[:8]).strip() if result else ""


def _extract_section(content: str, keyword: str) -> str:
    lines = content.split('\n')
    result = []
    capturing = False
    for line in lines:
        if keyword.upper() in line.upper():
            capturing = True
        elif capturing and re.match(r'^\d+\.', line.strip()) and result:
            break
        if capturing:
            result.append(line)
        if capturing and len(result) > 12:
            break
    return '\n'.join(result).strip() if result else f"Ver análisis completo para detalles de {keyword}."


def _extract_time_estimation(content: str) -> str:
    """Intenta extraer tiempos reales del contenido, si no usa defaults."""
    time_data = {}

    patterns = {
        "planificacion": [r"planificaci[oó]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "diseno_casos": [r"dise[nñ]o[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "preparacion_entorno": [r"preparaci[oó]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "ejecucion": [r"ejecuci[oó]n[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
        "reporte_cierre": [r"reporte[^\d]*(\d+)[^\d]*(\d+)[^\d]*(\d+)"],
    }

    for key, pats in patterns.items():
        for pat in pats:
            match = re.search(pat, content.lower())
            if match:
                opt, prob, pes = match.group(1), match.group(2), match.group(3)
                time_data[key] = f"{opt}-{pes} dias"
                break
        if key not in time_data:
            defaults = {
                "planificacion": "3-5 dias",
                "diseno_casos": "4-6 dias",
                "preparacion_entorno": "2-3 dias",
                "ejecucion": "8-12 dias",
                "reporte_cierre": "2-3 dias"
            }
            time_data[key] = defaults.get(key, "2-5 dias")

    # Intentar extraer totales
    total_match = re.search(r'optimista[:\s]*(\d+)\s*d[ií]as', content.lower())
    time_data["total_optimista"] = f"{total_match.group(1)} dias" if total_match else "19-22 dias"

    total_match = re.search(r'probable[:\s]*(\d+)\s*d[ií]as', content.lower())
    time_data["total_probable"] = f"{total_match.group(1)} dias" if total_match else "28-34 dias"

    total_match = re.search(r'pesimista[:\s]*(\d+)\s*d[ií]as', content.lower())
    time_data["total_pesimista"] = f"{total_match.group(1)} dias" if total_match else "38-43 dias"

    return json.dumps(time_data, ensure_ascii=False)