from pydantic_settings import BaseSettings
from functools import lru_cache


class Settings(BaseSettings):
    ai_provider: str = "claude"
    anthropic_api_key: str = ""
    claude_model: str = "claude-opus-4-5"
    openai_api_key: str = ""
    openai_model: str = "gpt-4o"
    host: str = "0.0.0.0"
    port: int = 8000
    debug: bool = True
    api_secret_key: str = "qa-copilot-secret"
    allowed_origins: str = "http://localhost:5199,http://localhost:4200"

    class Config:
        env_file = ".env"
        case_sensitive = False


@lru_cache()
def get_settings() -> Settings:
    return Settings()