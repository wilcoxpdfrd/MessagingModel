namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    /// <summary>
    /// Enumeration whose values specifiy whether requests are wrapped with the the resource action name, 
    /// and responses are wrapped with the resource action name with "Result" appended.
    /// </summary>
    public enum ResourceActionStyle
    {
        //
        // Summary:
        //     Neither requests nor responses are wrapped.
        Bare = 0,
        //
        // Summary:
        //     Both requests and responses are wrapped.
        Wrapped = 1,
        //
        // Summary:
        //     Requests are wrapped, responses are not wrapped.
        WrappedRequest = 2,
        //
        // Summary:
        //     Responses are wrapped, requests are not wrapped.
        WrappedResponse = 3
    }
}