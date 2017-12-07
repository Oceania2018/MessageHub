# Chat365-Core
Backend of realtime bi-direcational live message startkit.

How to setup:

1. Add line to ConfigureServices

  services.AddSignalR();
  
2. Setup cross-domain

  app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());


3. Add authentication middleware
   
   Please reference WebStart.Startup.


4. Add GetUserProfile delegate

  app.UseSignalR(config => {
                config.MapHub<MessageHub>(String.Empty);
                MessageHubInitializer.Init((user) => {
                    user.Name = "haiping";
                    user.FirstName = "Haiping";
                    user.LastName = "Chen";
                });
            });
