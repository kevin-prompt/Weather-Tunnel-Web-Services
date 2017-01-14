using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Xml;

namespace WSHelpers
{
    /// <summary>
    /// This subclassing of the WebServiceHostFactory allows us to replace some of the built in Behaviors.
    /// NOTE: The speicific class getting generated must be specified when creating a ServiceHost.  All other
    /// code can be left as is.  This assumes all output is in xml.
    /// THIS MEANS MULTIPLE .SVC INTERFACES NEED MULTIPLE OVERRIDE CLASSES (they can all be in this file).
    /// </summary>
    public class WSHFactoryOverrideCL : WebServiceHostFactory
    {
        public override ServiceHostBase CreateServiceHost(string service, Uri[] baseAddresses)
        {
            ServiceHost host = new ServiceHost(typeof(Coolftc.WTClimate.Climate), baseAddresses);
            // Since we support multiple endpoints, make sure you get the right kind to override.
            for (int i = 0; i < host.Description.Endpoints.Count; ++i)
            {
                if (host.Description.Endpoints[i].Name.IndexOf("webHttpBinding", 0, StringComparison.InvariantCultureIgnoreCase) > -1)
                    host.Description.Endpoints[i].Behaviors.Add(new WebHttpBehaviorEx());
            }
            return host;
        }
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return base.CreateServiceHost(serviceType, baseAddresses);
        }
    }

    public class WSHFactoryOverrideSP : WebServiceHostFactory
    {
        public override ServiceHostBase CreateServiceHost(string service, Uri[] baseAddresses)
        {
            ServiceHost host = new ServiceHost(typeof(Coolftc.WTClimate.Support), baseAddresses);
            // Since we support multiple endpoints, make sure you get the right kind to override.
            for (int i = 0; i < host.Description.Endpoints.Count; ++i)
            {
                if (host.Description.Endpoints[i].Name.IndexOf("webHttpBinding", 0, StringComparison.InvariantCultureIgnoreCase) > -1)
                    host.Description.Endpoints[i].Behaviors.Add(new WebHttpBehaviorEx());
            }
            return host;
        }
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return base.CreateServiceHost(serviceType, baseAddresses);
        }
    }

    /// <summary>
    /// This subclassing of the WebHttpBehavior (REST) allows us to override the automatic generation of a web
    /// page when an exception is thrown and replace it with some standard output.
    /// </summary>
    public class WebHttpBehaviorEx : WebHttpBehavior
    {
        
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // Remove any existing Error Handlers
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Clear();
            // Put in the desired Error Handler
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new WebHttpErrorHandler());
        }
    }

    /// <summary>
    /// Formate the outgoing header and body based on information in the Exception.
    /// </summary>
    public class WebHttpErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception exp, MessageVersion ver, ref Message fault)
        {
            // Take the exception error message and put it in the output area
            fault = Message.CreateMessage(ver, "", exp.Message, new DataContractSerializer(exp.Message.GetType()));
            
            // Specify the status code
            var header = new HttpResponseMessageProperty();
            header.StatusCode = System.Net.HttpStatusCode.InternalServerError;
            fault.Properties.Add(HttpResponseMessageProperty.Name, header);
        }
    }

}