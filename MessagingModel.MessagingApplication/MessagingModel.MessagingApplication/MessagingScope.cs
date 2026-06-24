using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public abstract class MessagingScope : IDisposable
    {
        private bool disposedValue;
        private IServiceScope scope;
        private IServiceProvider scopedServiceProvider;

        /// <summary>
        /// Gets or sets the System.IServiceProvider that provides access to the messaging handler's service container.
        /// </summary>
        public virtual IServiceProvider Services { get => this.scopedServiceProvider; set => this.scopedServiceProvider = ConfigureScopedProvider(value); }
        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this request.
        /// </summary>
        public virtual IDictionary<object, object> Items { get; set; }

        private IServiceProvider ConfigureScopedProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)

                return null;

            this.scope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope();

            return this.scope.ServiceProvider;
        }


        /// <summary>
        /// Disposes <see cref="Services"/>, <see cref="Items"/>.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.scopedServiceProvider = null;
                    if (this.scope != null)
                    {
                        this.scope.Dispose();
                        this.scope = null;
                    }
                    if (this.Items != null)
                    {
                        this.Items.Clear();
                        this.Items = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MessageHandlerContext()
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
