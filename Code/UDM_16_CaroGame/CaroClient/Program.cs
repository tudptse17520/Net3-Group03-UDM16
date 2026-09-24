namespace CaroClient
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            using var context = new CaroApplicationContext();
            Application.Run(context);
        }
    }
}
