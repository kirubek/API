using BCrypt.Net;

var password = "BaseOps@2026";
var storedHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYkP5PqZ0iK";

Console.WriteLine($"Testing password: {password}");
Console.WriteLine($"Stored hash: {storedHash}");
Console.WriteLine($"Hash length: {storedHash.Length}");

var isValid = BCrypt.Verify(password, storedHash);
Console.WriteLine($"Verification result: {isValid}");

// Generate a new hash to compare
var newHash = BCrypt.HashPassword(password, workFactor: 12);
Console.WriteLine($"New hash: {newHash}");
Console.WriteLine($"New hash length: {newHash.Length}");

var isNewValid = BCrypt.Verify(password, newHash);
Console.WriteLine($"New hash verification: {isNewValid}");
