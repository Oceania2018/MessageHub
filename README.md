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
  
5. Invoke from JS
  <script>
    var signalrClient = require('./libs/signalr-clientES5');
    let signalr = new signalrClient.HubConnection('http://localhost:9000?userId=haiping');
    signalr.send = function(data) {
      signalr.invoke('received', data);
    }

    signalr.start().then(() => console.log('connected'));
    
    // send message to specific channel, message will go to public channel if channelId is empty
    signalr.send({channelId: '', message: {text: 'Hello world'}});
    
    // received message event
    signalr.on('received', data => {
      console.log(data);
    });
  </script>
