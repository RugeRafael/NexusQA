namespace QACopilot.Application.Interfaces.Services;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string HashSensitiveData(string data);
}