﻿namespace Zilon.Core.Services.CombatEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Zilon.Core.Tactics.Events;

    public class EventManager : IEventManager
    {
        private readonly Dictionary<string, List<ITacticEvent>> eventDict;
        private readonly List<ITacticEvent> launchEvents;
        private readonly List<ITacticEvent> waitingEvents;
        private bool processingBegins;

        public event EventHandler<CombatEventArgs> OnEventProcessed;


        public EventManager()
        {
            eventDict = new Dictionary<string, List<ITacticEvent>>();
            launchEvents = new List<ITacticEvent>();
            waitingEvents = new List<ITacticEvent>();
        }

        public void SetEvents(ITacticEvent[] events)
        {
            eventDict.Clear();
            launchEvents.Clear();
            processingBegins = true;
            EventsToQueue(events);
        }

        public void LaunchTargetEvents(ITacticEvent targetEvent, string[] names)
        {
        }

        public void Update()
        {
            while (launchEvents.Any())
            {
                var combatEvent = launchEvents.First();
                waitingEvents.Add(combatEvent);
                launchEvents.RemoveAt(0);
                ProcessEvent(combatEvent);
            }
        }

        private void ProcessEvent(ITacticEvent targetEvent)
        {
            var args = new CombatEventArgs
            {
                CommandEvent = targetEvent
            };

            args.OnComplete += (s, e)=> {
                ComplateEvent(targetEvent);
            };

            OnEventProcessed?.Invoke(this, args);
        }

        private void ComplateEvent(ITacticEvent targetEvent)
        {
            throw new NotImplementedException();
        }

        public void EventsToQueue(ITacticEvent[] events)
        {
            foreach (var combatEvent in events)
            {
                if (combatEvent.TriggerName != null)
                {
                    if (!eventDict.ContainsKey(combatEvent.TriggerName))
                    {
                        eventDict.Add(combatEvent.TriggerName, new List<ITacticEvent>());
                    }

                    eventDict[combatEvent.TriggerName].Add(combatEvent);
                }
                else
                {
                    launchEvents.Add(combatEvent);
                }
            }
        }
    }
}
