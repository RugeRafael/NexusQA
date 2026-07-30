import anthropic
from app.config import get_settings
from app.prompts.system_prompts import BASE_SYSTEM_PROMPT

settings = get_settings()


class ClaudeService:
    def __init__(self):
        self.client = anthropic.Anthropic(api_key=settings.anthropic_api_key)
        self.model = settings.claude_model

    async def generate(self, prompt: str, system_extra: str = "") -> tuple[str, int]:
        system = BASE_SYSTEM_PROMPT
        if system_extra:
            system += f"\n\n{system_extra}"

        message = self.client.messages.create(
            model=self.model,
            max_tokens=4096,
            system=system,
            messages=[{"role": "user", "content": prompt}]
        )

        content = message.content[0].text
        tokens = message.usage.input_tokens + message.usage.output_tokens
        return content, tokens

    async def generate_with_history(
        self, messages: list[dict], system_extra: str = ""
    ) -> tuple[str, int]:
        # La API de Anthropic NO acepta role "system" dentro de messages.
        # Si el caller (ai_service.py) incluyo un mensaje system (con contexto RAG),
        # lo extraemos y lo pasamos por el parametro system= correcto.
        clean_messages = list(messages)
        system = BASE_SYSTEM_PROMPT

        if clean_messages and clean_messages[0].get("role") == "system":
            # Usar el contenido del system embebido (incluye contexto RAG) en vez del BASE_SYSTEM_PROMPT plano
            system = clean_messages[0]["content"]
            clean_messages = clean_messages[1:]

        if system_extra:
            system += f"\n\n{system_extra}"

        # Anthropic requiere que el primer mensaje sea "user". Si por alguna razon
        # queda vacio o empieza con "assistant", lo protegemos.
        if not clean_messages or clean_messages[0].get("role") != "user":
            clean_messages = [{"role": "user", "content": "Continuemos."}] + clean_messages

        response = self.client.messages.create(
            model=self.model,
            max_tokens=2048,
            system=system,
            messages=clean_messages
        )

        content = response.content[0].text
        tokens = response.usage.input_tokens + response.usage.output_tokens
        return content, tokens
