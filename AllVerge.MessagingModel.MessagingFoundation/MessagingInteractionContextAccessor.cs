using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    /// <summary>
    /// Provides access to the execution context of a messaging interaction.
    /// </summary>
    public class MessagingInteractionContextAccessor
    {
        internal class Holder
        {
            private MessagingInteractionContextAccessor context;

            public MessagingInteractionContextAccessor Context
            {
                get
                {
                    return this.context;
                }
                set
                {
                    this.context = value;
                }
            }
        }

        private static AsyncLocal<MessagingInteractionContextAccessor.Holder> currentContext = new AsyncLocal<Holder>();
        private MessagingInteractionContext interactionContext;

        internal static MessagingInteractionContextAccessor.Holder CurrentHolder
        {
            get
            {
                MessagingInteractionContextAccessor.Holder holder = MessagingInteractionContextAccessor.currentContext.Value;
                if (holder == null)
                {
                    holder = new MessagingInteractionContextAccessor.Holder();
                    MessagingInteractionContextAccessor.currentContext.Value = holder;
                }
                return holder;
            }
        }

        /// <summary>Gets or sets the execution context for the current thread.</summary>
        /// <returns>The <see cref="MessagingInteractionContextAccessor" /> that represents the messaging and execution context of the current method.</returns>
        public static MessagingInteractionContextAccessor Current
        {
            get
            {
                return MessagingInteractionContextAccessor.CurrentHolder.Context;
            }
            
            set
            {
                MessagingInteractionContextAccessor.CurrentHolder.Context = value;
            }
        }

        /// <summary>Gets the <see cref="Messaging.InteractionContext" /> object that manages the current messaging interaction context.</summary>
        /// <returns>The <see cref="Messaging.InteractionContext" /> object for the current messaging interaction context.</returns>
        public MessagingInteractionContext InteractionContext
        {
            get
            {
                return this.interactionContext;
            }
        }

        internal void SetInteractionContext(MessagingInteractionContext interactionContext)
        {
            this.interactionContext = interactionContext;
        }
    }
}
