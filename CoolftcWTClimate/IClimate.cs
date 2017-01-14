using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Xml;

namespace Coolftc.WTClimate
{
    /// <summary>
    /// The Climate API provides an interface to retrieve climate and location information. The Climate API creates
    /// a aggregation point for multiple data sources.  It provides faster and more cost effective data than going 
    /// directly to the sources from the client device.
    /// </summary>
    [ServiceContract(Name = "Climate", Namespace = "http://coolftc.org/CoolftcWTClimate")]
    public interface IClimate
    {
        /// <summary>
        /// Set up user with a unique id for use as a ticket.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/weather/register/{ali}?ticket={ticket}")]
        XmlElement GetRegistration(string ticket, string ali);

        /// <summary>
        /// Return meteorological information for the requested location.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/weather/{station}?ticket={ticket}")]
        XmlElement GetWeather(string ticket, string station);

        /// <summary>
        /// Return non-forecast meteorological information for the requested location.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/weather/detail/{station}?ticket={ticket}")]
        XmlElement GetWeatherDL(string ticket, string station);

        /// <summary>
        /// Return very recent non-forecast meteorological information for the requested location.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/weather/force/{station}?ticket={ticket}")]
        XmlElement GetWeatherFR(string ticket, string station);

        /// <summary>
        /// Return summary of meteorological information for the requested location.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/weather/slim/{station}?ticket={ticket}")]
        XmlElement GetWeatherSL(string ticket, string station);

        /// <summary>
        /// Return location information for the requested locality.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/location/search/{link}?ticket={ticket}")]
        XmlElement GetLocation(string ticket, string link);

        /// <summary>
        /// Return all the destinations + coordinates for the prepaq.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/location/prepaq/{prepaq}?ticket={ticket}")]
        XmlElement GetPrepaqList(string ticket, string prepaq);

        /// <summary>
        /// Return the Version information of this web service.
        /// </summary>
        [OperationContract]
        [WebGet(UriTemplate = "v1/utility/version")]
        string Version();
    }

}