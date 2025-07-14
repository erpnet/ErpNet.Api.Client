using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ErpNet.Api.Client
{
    /// <summary>
    /// A model class for {dbhost}/sys/auto-discovery endpoint.
    /// </summary>
    public class AutoDiscovery
    {
        /// <summary>
        /// A collection of web sites for the destination database.
        /// </summary>
        public AutoDiscoveryWebSite[]? WebSites { get; set; }
    }

    /// <summary>
    /// A model class for {dbhost}/sys/auto-discovery endpoint.
    /// </summary>
    [DebuggerDisplay("{Type} [{Status}] => {Url}")]
    public class AutoDiscoveryWebSite
    {
        /// <summary>
        /// The type of web site.
        /// </summary>
        [JsonConverter(typeof(WebsiteTypeJsonConverter))]
        public WebsiteType Type { get; set; } = WebsiteType.Unknown;

        /// <summary>
        /// Working status.
        /// </summary>
        public WebsiteStatus Status { get; set; }

        /// <summary>
        /// Url of the web ite.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Additional properties specific for the web site.
        /// </summary>
        public Dictionary<string, string>? AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Type of web site. This enum must be euivalent to WebSitesRepository.WebSiteType enum.
    /// </summary>
    public enum WebsiteType
    {
        /// <summary>
        /// An unknown website type. Often these are new ones.
        /// </summary>
        Unknown,

        // System types
        /// <summary>
        /// ID value. Stored as 'ID'. Open ID Provider.
        /// </summary>
        ID,

        /// <summary>
        /// UserProfile value. Stored as 'PROFILE'.
        /// </summary>
        UserProfile,

        /// <summary>
        /// Instance manager system type.
        /// </summary>
        InstanceManager,

        /// <summary>
        /// Allows external applications to access the ERP resources using Domain API.. Stored as &apos;API&apos;
        /// </summary>
        DomainAPI,

        /// <summary>
        /// Allows community users to access ERP resources. Requires working ID site.. Stored as &apos;CC&apos;
        /// </summary>
        ClientCenter,

        /// <summary>
        /// ECommerce value. Stored as &apos;EC&apos;
        /// </summary>
        ECommerce,

        /// <summary>
        /// LEGALBG value. Stored as &apos;LEG&apos;.
        /// </summary>
        LegalBG,

        /// <summary>
        /// SocialInteractions value. Stored as &apos;SI&apos;.
        /// </summary>
        SocialInteractions,

        /// <summary>
        /// DigitalMarketplace value. Stored as &apos;DM&apos;.
        /// </summary>
        DigitalMarketplace,

        /// <summary>
        /// WebClient value. Stored as &apos;APP&apos;.
        /// </summary>
        WebClient,

        /// <summary>
        /// TableAPI value. Stored as &apos;TAP&apos;.
        /// </summary>
        TableAPI,

        /// <summary>
        /// DataAccessAPI value. Stored as &apos;DAP&apos;.
        /// </summary>
        DataAccessAPI,

        /// <summary>
        /// LEGALUK value. Stored as &apos;LUK&apos;.
        /// </summary>
        LegalUK,

        /// <summary>
        /// OLAP value. Stored as &apos;OLP&apos;.
        /// </summary>
        OLAP,

        /// <summary>
        /// MicrosoftSync value. Stored as &apos;MSS&apos;.
        /// </summary>
        MicrosoftSync,

        /// <summary>
        /// VideoConference value. Stored as &apos;MET&apos;.
        /// </summary>
        VideoConferencing,

        /// <summary>
        /// AIServer value. Stored as &apos;AIS&apos;
        /// </summary>
        AIServer,
    }

    /// <summary>
    /// Information about the web site
    /// </summary>
    public enum WebsiteStatus
    {
        /// <summary>
        /// We haven't been able to determine the status yet
        /// </summary>
        Unknown,
        /// <summary>
        /// Process is working and we are able to ping the web site
        /// </summary>
        Working,
        /// <summary>
        /// Site is down, the process is unresponsive, or we can't ping the web site
        /// </summary>
        NotWorking
    }
}
