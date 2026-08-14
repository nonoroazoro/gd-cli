namespace GdCli.Application;

internal enum ExitCode
{
    Success = 0,
    UnexpectedError = 1,
    InvalidArguments = 2,
    DatabaseNotFound = 3,
    IncompatibleDatabase = 4,
    RecordNotFound = 5
}
