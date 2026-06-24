using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Provides a context for binding to a messaging channel.
    /// </summary>
    public class BindingContext : IDisposable
    {
        private bool disposedValue;

        public BindingContext()
        { 
            this.ConnectionContext = new ConnectionContext();
            this.InteractionContext = new InteractionContext();
        }

        public ConnectionContext ConnectionContext { get; private set; }

        public InteractionContext InteractionContext { get; private set; }

        public bool TryApply(BindingContext bindingContext)
        {
            if (this.disposedValue)

                throw new ObjectDisposedException(this.GetType().Name);

            if (bindingContext != null)
            {
                ConnectionContext.Map(this.ConnectionContext, bindingContext.ConnectionContext);

                InteractionContext.Map(this.InteractionContext, bindingContext.InteractionContext.InputHeaders, bindingContext.InteractionContext.InputLocation, bindingContext.InteractionContext.InputVerb, DateTimeOffset.Now, $"{bindingContext.InteractionContext.TraceIdentifier}:{bindingContext.InteractionContext.ToString()}", bindingContext.InteractionContext.User);

                return true;
            }
            
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.ConnectionContext.Dispose();

                    this.ConnectionContext = null;

                    this.InteractionContext.Dispose();

                    this.InteractionContext = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BindingContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
