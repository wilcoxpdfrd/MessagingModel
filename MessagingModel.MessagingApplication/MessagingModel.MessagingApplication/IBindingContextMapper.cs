namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Used for mapping a protocol aware context to another context, such as a channel binding context.
    /// </summary>
    /// <typeparam name="TContext">A protocol context to be mapped to a <see cref="BindingContext"/>.</typeparam>
    public interface IBindingContextMapper<TContext>
    {
        /// <summary>
        /// Maps a protocol context aware <typeparamref name="TContext"/> <paramref name="context"/> to a <see cref="BindingContext"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bindingContext"></param>
        bool MapToBindingContext(TContext context, BindingContext bindingContext);
    }
}