﻿using Chronos.Core.Reflection;
using Chronos.Server.Network;
using Chronos.Protocol.Enums;
using Chronos.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chronos.Protocol.Messages.StateMessages;

namespace Chronos.Server.Handlers
{
    public class PacketManager
    {
        public static Dictionary<HeaderEnum, Action<object, SimpleClient, NetworkMessage>> MethodHandlers = new Dictionary<HeaderEnum, Action<object, SimpleClient, NetworkMessage>>();
        public static Dictionary<StateTypeEnum, Action<object, SimpleClient, StateDataMessage>> StateMethodHandlers = new Dictionary<StateTypeEnum, Action<object, SimpleClient, StateDataMessage>>();
        public static void Initialize(Assembly asm)
        {
            var methods = asm.GetTypes()
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(HeaderPacketAttribute), false).Length > 0)
                      .ToArray();
            foreach (var method in methods)
            {
                var action =  DynamicExtension.CreateDelegate(method, typeof(SimpleClient), typeof(NetworkMessage)) as Action<object, SimpleClient, NetworkMessage>;
                MethodHandlers.Add((HeaderEnum)method.CustomAttributes.ToArray()[0].ConstructorArguments[0].Value, action);
            }

            methods = asm.GetTypes()
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(StateIdAttribute), false).Length > 0)
                      .ToArray();
            foreach (var method in methods)
            {
                var action = DynamicExtension.CreateDelegate(method, typeof(SimpleClient), typeof(StateDataMessage)) as Action<object, SimpleClient, StateDataMessage>;
                StateMethodHandlers.Add((StateTypeEnum)method.CustomAttributes.ToArray()[0].ConstructorArguments[0].Value, action);
            }
        }
        public static void ParseHandler(SimpleClient client, NetworkMessage message)
        {
            try
            {
                if (message != null)
                {
                    if (MethodHandlers.TryGetValue((HeaderEnum)message.MessageId, out var methodToInvok))
                    {
                        methodToInvok.Invoke(null, client, message);
                    }
                    else
                    {
                        Console.WriteLine($"Received non handled Packet : id = {message.MessageId} -> {message}");
                    }
                }
                else
                {
                    Console.WriteLine("Receive empty packet");
                    client.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void ParseStateHandler(SimpleClient client, StateDataMessage message)
        {
            try
            {
                if (message != null)
                {
                    if (StateMethodHandlers.TryGetValue((StateTypeEnum)message.MessageId, out var methodToInvok))
                    {
                        methodToInvok.Invoke(null, client, message);
                    }
                    else
                    {
                        Console.WriteLine($"Received non handled state : id = {message.MessageId} -> {message}");
                    }
                }
                else
                {
                    Console.WriteLine("Receive empty state");
                    client.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
