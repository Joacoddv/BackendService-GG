using System.Runtime.CompilerServices;

// Allow the test project to access internal types (e.g. GastroGestionExceptionHandler)
// so that integration tests can register the real handler in focused TestServer instances.
[assembly: InternalsVisibleTo("GastroGestion.Api.Tests")]
