using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace RealtimeHub.TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var roomId = args.Length > 0 ? args[0] : "R1";
            var userId = args.Length > 1 ? args[1] : "U1";
            
            Console.WriteLine($"[TestClient] Connecting to Room: {roomId}, User: {userId}...");

            var connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:5056/gameHub?roomId={roomId}&userId={userId}")
                .Build();

            string[] events = new[] {
                "onRoomCreated", "onParticipantJoined", "onDraftReady", "onWeekAdvanceReady",
                "onPlayerClaimed", "onPlayerClaimRejected", "onDraftTurnAdvanced",
                "onDraftCompleted", "onMatchResult", "onWeekAdvanced", "onSponsorshipOffered"
            };

            foreach(var ev in events)
            {
                var eventName = ev; // capture
                connection.On<object>(eventName, (payload) => 
                {
                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine($"\n>>> EVENT RECEIVED: [{eventName}]");
                    Console.WriteLine(json);
                });
            }

            try
            {
                await connection.StartAsync();
                Console.WriteLine("[TestClient] ✅ Connected successfully!");
                Console.WriteLine("[TestClient] Waiting for events... Press Ctrl+C to exit.");
                
                // Signal completion to the powershell script so it can proceed
                Console.WriteLine("READY_TO_TEST");
                
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestClient] ❌ Connection Error: {ex.Message}");
            }
        }
    }
}
