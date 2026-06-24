namespace AllVerge.MessagingModel.Description.Model
{
    public interface ISpecifiesBindings
    {
        BindingProperties Bindings { get; set; }
        bool BindingsSpecified { get; }
        bool ShouldSerializeBindings();
    }
}