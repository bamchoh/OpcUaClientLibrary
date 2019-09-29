using System;
using System.Linq;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Client;

namespace OpcUa
{
    public class OpcUaClient : IDisposable
    {
        private readonly string APP_NAME = "OPC UA Client for Python";
        private const string DEFAULT_CONFIG_SECTION_NAME = "Opc.Ua.Client.Python";

        private Session session;

        public string ConfigSectionName { get; set; }

        public void Dispose()
        {
            if (session != null && session.Connected)
                session.Close();
        }

        public bool Open(string endpointURI, bool useSecurity = false, string username = "", string password = "")
        {
            // load the application configuration.
            var config = InitConfig();
            var uid = GetUserIdentity(username, password);
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURI, useSecurity, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var configureEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            this.session = Session.Create(
                config,
                configureEndpoint,
                false,
                APP_NAME,
                60000,
                uid,
                null).Result;
            return true;
        }

        public void Close()
        {
            this.session?.Close();
        }

        public Dictionary<string, DataValue> Read(IEnumerable<string> keys)
        {
            if (this.session == null)
                return null;

            var items = new ReadValueIdCollection();

            foreach (var id in keys)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                items.Add(new ReadValueId()
                {
                    NodeId = new NodeId(id),
                    AttributeId = Attributes.Value,
                    IndexRange = null,
                    DataEncoding = null,
                });
            }

            var results = new DataValueCollection();
            var diagnosticInfos = new DiagnosticInfoCollection();

            this.session.Read(
                null,
                0,
                TimestampsToReturn.Both,
                items,
                out results,
                out diagnosticInfos
            );

            var tmp = new Dictionary<string, DataValue>();

            for (int i = 0; i < keys.Count(); i++)
            {
                var id = keys.ElementAt(i);
                var value = results[i];
                tmp[id] = value;
            }

            return tmp;
        }

        public bool Write(Dictionary<string, object> items)
        {
            if (this.session == null)
                return false;

            var dataValueCollection = Read(items.Keys);

            var writeValues = new WriteValueCollection();

            foreach (var dataValue in dataValueCollection)
            {
                var value = new WriteValue()
                {
                    NodeId = new NodeId(dataValue.Key),
                    AttributeId = Attributes.Value,
                    Value = new DataValue()
                    {
                        StatusCode = StatusCodes.Good,
                        ServerTimestamp = DateTime.MinValue,
                        SourceTimestamp = DateTime.MinValue,
                        Value = DetermineType(items[dataValue.Key], dataValue.Value),
                    },
                };

                writeValues.Add(value);
            }

            StatusCodeCollection results = null;
            DiagnosticInfoCollection diagnosticInfos = null;

            ResponseHeader responseHeader = this.session.Write(
                null,
                writeValues,
                out results,
                out diagnosticInfos);

            ClientBase.ValidateResponse(results, writeValues);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, writeValues);

            return true;
        }

        private ApplicationConfiguration InitConfig()
        {
            var application = new ApplicationInstance
            {
                ApplicationName = APP_NAME,
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = ConfigSectionName ?? DEFAULT_CONFIG_SECTION_NAME,
            };

            var config = application.LoadApplicationConfiguration(false).Result;
            config.CertificateValidator.CertificateValidation +=
                new CertificateValidationEventHandler((sender, e) => e.Accept = true);

            return config;
        }

        private UserIdentity GetUserIdentity(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new UserIdentity(new AnonymousIdentityToken());
            }

            if (string.IsNullOrEmpty(password))
            {
                password = "";
            }
            return new UserIdentity(username, password);
        }

        private EndpointDescriptionCollection Discover(string discoveryURL, int operationTimeout)
        {
            Uri uri = new Uri(discoveryURL);

            EndpointConfiguration configuration = EndpointConfiguration.Create();
            if (operationTimeout > 0)
            {
                configuration.OperationTimeout = operationTimeout;
            }

            var endpoints = new EndpointDescriptionCollection();

            using (DiscoveryClient client = DiscoveryClient.Create(uri, configuration))
            {
                foreach (var got in client.GetEndpoints(null))
                {
                    if (got.EndpointUrl.StartsWith(uri.Scheme))
                    {
                        endpoints.Add(got);
                    }
                }
            }

            return endpoints;
        }

        private object DetermineType(object value, DataValue dataValue)
        {
            switch (dataValue.WrappedValue.TypeInfo.BuiltInType)
            {
                case BuiltInType.Boolean:
                    return Convert.ToBoolean(value);
                case BuiltInType.SByte:
                    return Convert.ToSByte(value);
                case BuiltInType.Byte:
                    return Convert.ToByte(value);
                case BuiltInType.Int16:
                    return Convert.ToInt16(value);
                case BuiltInType.Int32:
                    return Convert.ToInt32(value);
                case BuiltInType.Int64:
                    return Convert.ToInt64(value);
                case BuiltInType.UInt16:
                    return Convert.ToUInt16(value);
                case BuiltInType.UInt32:
                    return Convert.ToUInt32(value);
                case BuiltInType.UInt64:
                    return Convert.ToUInt64(value);
                case BuiltInType.Float:
                    return Convert.ToSingle(value);
                case BuiltInType.Double:
                    return Convert.ToDouble(value);
                default:
                    return value;
            }
        }
    }
}
