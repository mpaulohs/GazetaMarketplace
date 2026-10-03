using Microsoft.VisualStudio.TestTools.UnitTesting;

// Um SQL Server só para a suíte: classes em paralelo (cada uma com o próprio banco), métodos da mesma classe em sequência
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
