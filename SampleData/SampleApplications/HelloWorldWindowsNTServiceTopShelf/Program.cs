namespace HelloWorldTopshelf;

public static class Program
{
    public static void Main()
    {
        HostFactory.Run(x =>
        {
            x.Service<HelloWorldService>(s =>
            {
                s.ConstructUsing(name => new HelloWorldService());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());
            });
            x.RunAsLocalSystem();

            x.SetDescription("HelloWorldService");
            x.SetDisplayName("HelloWorldService");
            x.SetServiceName("HelloWorldService");
        });
    }
}