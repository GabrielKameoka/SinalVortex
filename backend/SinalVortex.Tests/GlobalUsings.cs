// SinalVortex.Tests/GlobalUsings.cs
global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using FluentAssertions;
global using Microsoft.Extensions.Time.Testing;
global using Moq;
global using Polly;
global using Polly.CircuitBreaker;
global using Xunit;

// Força a resolução global do TimeProvider para o tipo nativo do BCL no .NET 10
global using TimeProvider = System.TimeProvider;