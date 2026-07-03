from openai import OpenAI
from app.config import get_settings
from app.prompts.system_prompts import BASE_SYSTEM_PROMPT

settings = get_settings()


class OpenAIService:
    def __init__(self):
        self.client = OpenAI(api_key=settings.openai_api_key)
        self.model = settings.openai_model

    async def generate(self, prompt: str, system_extra: str = "") -> tuple[str, int]:
        system = BASE_SYSTEM_PROMPT
        if system_extra:
            system += f"\n\n{system_extra}"

        response = self.client.chat.completions.create(
            model=self.model,
            max_tokens=4096,
            messages=[
                {"role": "system", "content": system},
                {"role": "user", "content": prompt}
            ]
        )

        content = response.choices[0].message.content or ""
        tokens = response.usage.total_tokens if response.usage else 0

        return content, tokens

    async def generate_with_history(
        self, messages: list[dict], system_extra: str = ""
    ) -> tuple[str, int]:
        system = BASE_SYSTEM_PROMPT
        if system_extra:
            system += f"\n\n{system_extra}"

        full_messages = [{"role": "system", "content": system}] + messages

        response = self.client.chat.completions.create(
            model=self.model,
            max_tokens=2048,
            messages=full_messages
        )

        content = response.choices[0].message.content or ""
        tokens = response.usage.total_tokens if response.usage else 0

        return content, tokens