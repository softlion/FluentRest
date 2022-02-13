using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentRest.Http.Testing;
using FluentRest.Rest.Configuration;

namespace FluentRest.Http.Configuration
{
    /// <summary>
    /// A set of properties that affect FluentRest.Http behavior
    /// </summary>
    public class FluentRestHttpSettings
    {
        // Values are dictionary-backed so we can check for key existence. Can't do null-coalescing
        // because if a setting is set to null at the request level, that should stick.
        private readonly IDictionary<string, object?> values = new Dictionary<string, object?>();

        private FluentRestHttpSettings? defaults;

        /// <summary>
        /// Creates a new FluentRestHttpSettings object.
        /// </summary>
        public FluentRestHttpSettings() 
        {
            Redirects = new RedirectSettings(this);
            ResetDefaults();
        }
        /// <summary>
        /// Gets or sets the default values to fall back on when values are not explicitly set on this instance.
        /// </summary>
        public virtual FluentRestHttpSettings Defaults 
        {
            get => defaults ?? FluentRestHttp.GlobalSettings;
            set => defaults = value;
        }

        /// <summary>
        /// Gets or sets the HTTP request timeout.
        /// </summary>
        public TimeSpan? Timeout {
            get => Get<TimeSpan?>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a pattern representing a range of HTTP status codes which (in addtion to 2xx) will NOT result in FluentRest.Http throwing an Exception.
        /// Examples: "3xx", "100,300,600", "100-299,6xx", "*" (allow everything)
        /// 2xx will never throw regardless of this setting.
        /// </summary>
        public string AllowedHttpStatusRange {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets object used to serialize and deserialize JSON. Default implementation uses Newtonsoft Json.NET.
        /// </summary>
        public ISerializer JsonSerializer {
            get => Get<ISerializer>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets object used to serialize URL-encoded data. (Deserialization not supported in default implementation.)
        /// </summary>
        public ISerializer UrlEncodedSerializer {
            get => Get<ISerializer>();
            set => Set(value);
        }

        /// <summary>
        /// Gets object whose properties describe how FluentRest.Http should handle redirect (3xx) responses.
        /// </summary>
        public RedirectSettings Redirects { get; }

        /// <summary>
        /// Gets or sets a callback that is invoked immediately before every HTTP request is sent.
        /// </summary>
        public Action<FluentRestDetail> BeforeCall {
            get => Get<Action<FluentRestDetail>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked asynchronously immediately before every HTTP request is sent.
        /// </summary>
        public Func<FluentRestDetail, Task> BeforeCallAsync {
            get => Get<Func<FluentRestDetail, Task>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked immediately after every HTTP response is received.
        /// </summary>
        public Action<FluentRestDetail> AfterCall {
            get => Get<Action<FluentRestDetail>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked asynchronously immediately after every HTTP response is received.
        /// </summary>
        public Func<FluentRestDetail, Task> AfterCallAsync {
            get => Get<Func<FluentRestDetail, Task>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked when an error occurs during any HTTP call, including when any non-success
        /// HTTP status code is returned in the response. Response should be null-checked if used in the event handler.
        /// </summary>
        public Action<FluentRestDetail> OnError {
            get => Get<Action<FluentRestDetail>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked asynchronously when an error occurs during any HTTP call, including when any non-success
        /// HTTP status code is returned in the response. Response should be null-checked if used in the event handler.
        /// </summary>
        public Func<FluentRestDetail, Task> OnErrorAsync {
            get => Get<Func<FluentRestDetail, Task>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked when any 3xx response with a Location header is received.
        /// You can inspect/manipulate the call.Redirect object to determine what will happen next.
        /// An auto-redirect will only happen if call.Redirect.Follow is true upon exiting the callback.
        /// </summary>
        public Action<FluentRestDetail> OnRedirect {
            get => Get<Action<FluentRestDetail>>();
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a callback that is invoked asynchronously when any 3xx response with a Location header is received.
        /// You can inspect/manipulate the call.Redirect object to determine what will happen next.
        /// An auto-redirect will only happen if call.Redirect.Follow is true upon exiting the callback.
        /// </summary>
        public Func<FluentRestDetail, Task> OnRedirectAsync {
            get => Get<Func<FluentRestDetail, Task>>();
            set => Set(value);
        }

        /// <summary>
        /// Resets all overridden settings to their default values. For example, on a FluentRestRequest,
        /// all settings are reset to FluentRestClient-level settings.
        /// </summary>
        public virtual void ResetDefaults() => values.Clear();

        /// <summary>
        /// Gets a settings value from this instance if explicitly set, otherwise from the default settings that back this instance.
        /// </summary>
        internal T Get<T>([CallerMemberName]string? propName = null) 
        {
            var testVals = HttpTest.Current?.Settings.values;
            return
                testVals?.ContainsKey(propName) == true ? (T)testVals[propName] :
                values.ContainsKey(propName) ? (T)values[propName] :
                Defaults != null ? (T)Defaults.Get<T>(propName) :
                default(T);
        }

        /// <summary>
        /// Sets a settings value for this instance.
        /// </summary>
        internal void Set<T>(T value, [CallerMemberName]string? propName = null) => values[propName!] = value;
    }
}