using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Invoiced
{
    public abstract class AbstractEntity<T> where T : AbstractEntity<T>
    {
        private bool _entityCreated;
        private Connection _connection;
        private string _CurrentOperation;
        // used to determine safe json serialisation. should always be null outside function bodies
        protected string CurrentOperation
        {
            get
            {
                return _CurrentOperation;
            }
            set
            {
                if (value != null && value.EndsWith("Async"))
                    _CurrentOperation = value.Substring(0, value.Length - 5);
                else
                    _CurrentOperation = value;
                    
            }
        }
        private string _endpointBase = "";
        protected string EntityName;

        internal AbstractEntity(Connection conn)
        {
            _connection = conn;
        }

        protected AbstractEntity()
        {
        }

        public bool ShouldSerializeCurrentOperation()
        {
            return false;
        }

        protected string GetEndpointBase()
        {
            return _endpointBase;
        }

        public void SetEndpointBase(string endpointBase)
        {
            _endpointBase = endpointBase;
        }

        public string GetEndpoint(bool includeId)
        {
            var url = GetEndpointBase() + EntityName;

            if (EntityId() != null && includeId) url += "/" + EntityId();

            return url;
        }

        public override string ToString()
        {
            var s = base.ToString() + "<" + EntityId() + ">";
            return s + " " + ToJsonString();
        }

        protected Connection GetConnection()
        {
            return _connection;
        }

        public void ChangeConnection(Connection conn)
        {
            _connection = conn;
        }

        #region "Async Methods"

        public virtual async System.Threading.Tasks.Task CreateAsync()
        {
            if (_entityCreated) throw new EntityException("Object has already been created.");

            if (!HasCrud()) throw new EntityException("Create operation not supported on object.");

            var url = GetEndpoint(false);
            var entityJsonBody = ToJsonString();

            var responseText = await _connection.PostAsync(url, null, entityJsonBody).ConfigureAwait(false);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            _entityCreated = true;
        }

        public virtual async System.Threading.Tasks.Task SaveAllAsync()
        {
            if (!HasCrud()) throw new EntityException("Save operation not supported on object.");

            var url = GetEndpoint(true);
            var entityJsonBody = ToJsonString();
            var responseText = await _connection.PatchAsync(url, entityJsonBody).ConfigureAwait(false);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        public async System.Threading.Tasks.Task SaveAsync(string partialDataObject)
        {
            if (!HasCrud()) throw new EntityException("Save operation not supported on object.");

            var url = GetEndpoint(true);
            var responseText = await _connection.PatchAsync(url, partialDataObject).ConfigureAwait(false);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        public Task<T> RetrieveAsync(long id)
        {
            return RetrieveAsync(id.ToString());
        }

        public async Task<T> RetrieveAsync(string id = null)
        {
            var url = GetEndpoint(false);

            if (id != null) url += "/" + id;

            var responseText = await _connection.GetAsync(url, null).ConfigureAwait(false);
            T serializedObject;
            try
            {
                serializedObject = JsonConvert.DeserializeObject<T>(responseText,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    });
                serializedObject._connection = _connection;
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            return serializedObject;
        }

        public virtual async System.Threading.Tasks.Task DeleteAsync()
        {
            if (!HasCrud()) throw new EntityException("Delete operation not supported on object.");

            await _connection.DeleteAsync(GetEndpoint(true)).ConfigureAwait(false);
        }

        public async Task<EntityList<T>> ListAsync(string nextUrl, Dictionary<string, object> queryParams,
            JsonConverter customConverter = null)
        {
            if (!HasList()) throw new EntityException("List operation not supported on object.");

            var url = GetEndpoint(false);

            if (!string.IsNullOrEmpty(nextUrl)) url = nextUrl;

            var response = await _connection.GetListAsync(url, queryParams).ConfigureAwait(false);

            EntityList<T> entities;

            var config = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            try
            {
                if (customConverter != null) config.Converters.Add(customConverter);
                entities = JsonConvert.DeserializeObject<EntityList<T>>(response.Result, config);

                entities.LinkURLS = response.Links;
                entities.TotalCount = response.TotalCount;
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            foreach (var entity in entities) entity.ChangeConnection(_connection);

            return entities;

        }

        public Task<EntityList<T>> ListAllAsync(Dictionary<string, object> queryParams, JsonConverter customConverter = null)
        {
            return ListAllAsync("", queryParams, customConverter);
        }

        public async Task<EntityList<T>> ListAllAsync(string nextUrl = "", Dictionary<string, object> queryParams = null,
            JsonConverter customConverter = null)
        {
            EntityList<T> entities = null;

            if (!HasList()) throw new EntityException("List operation not supported on object.");

            do
            {
                var tmpEntities = await ListAsync(nextUrl, queryParams, customConverter).ConfigureAwait(false);

                if (entities == null)
                {
                    entities = tmpEntities;
                    if (tmpEntities.TotalCount > 0)
                        entities.Capacity = tmpEntities.TotalCount;
                }
                else
                {
                    entities.AddRange(tmpEntities);
                    entities.LinkURLS = tmpEntities.LinkURLS;
                    entities.TotalCount = tmpEntities.TotalCount;
                }

                nextUrl = tmpEntities.GetNextURL();

            } while (!string.IsNullOrEmpty(entities.GetNextURL()) && entities.GetSelfURL() != entities.GetLastURL());

            return entities;
        }

        public async System.Threading.Tasks.Task VoidAsync()
        {
            if (!HasVoid()) throw new EntityException("Void operation not supported on object.");

            var url = GetEndpoint(true) + "/void";

            var responseText = await _connection.PostAsync(url, null, null).ConfigureAwait(false);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        public async Task<IList<Attachment>> ListAttachmentsAsync()
        {
            if (!HasAttachments()) throw new EntityException("List attachments operation not supported on object.");

            var url = GetEndpoint(true) + "/attachments";

            var responseText = await _connection.GetAsync(url, null).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IList<Attachment>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public async Task<IList<Email>> SendEmailAsync(EmailRequest emailRequest)
        {
            if (!HasSends()) throw new EntityException("Send email operation not supported on object.");

            var url = GetEndpoint(true) + "/emails";

            var jsonRequestBody = emailRequest.ToJsonString();

            var responseText = await _connection.PostAsync(url, null, jsonRequestBody).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IList<Email>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public async Task<Letter> SendLetterAsync(LetterRequest letterRequest = null)
        {
            if (!HasSends()) throw new EntityException("Send letter operation not supported on object.");

            string responseText;

            var url = GetEndpoint(true) + "/letters";

            if (letterRequest != null)
            {
                var jsonRequestBody = letterRequest.ToJsonString();
                responseText = await _connection.PostAsync(url, null, jsonRequestBody).ConfigureAwait(false);
            }
            else
            {
                responseText = _connection.Post(url, null, "");
            }

            return JsonConvert.DeserializeObject<Letter>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public async Task<IList<TextMessage>> SendTextAsync(TextRequest textRequest)
        {
            if (!HasSends()) throw new EntityException("Send text message operation not supported on object.");

            var url = GetEndpoint(true) + "/text_messages";

            var jsonRequestBody = textRequest.ToJsonString();

            var responseText = await _connection.PostAsync(url, null, jsonRequestBody).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IList<TextMessage>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        #endregion

        public virtual void Create()
        {
            if (_entityCreated) throw new EntityException("Object has already been created.");

            if (!HasCrud()) throw new EntityException("Create operation not supported on object.");

            var url = GetEndpoint(false);
            var entityJsonBody = ToJsonString();
            var responseText = _connection.Post(url, null, entityJsonBody);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            _entityCreated = true;
        }

        // this method serialises the existing object (with respect for defined create/update safety, i.e. ShouldSerialize functions)
        public virtual void SaveAll()
        {
            if (!HasCrud()) throw new EntityException("Save operation not supported on object.");

            var url = GetEndpoint(true);
            var entityJsonBody = ToJsonString();
            var responseText = _connection.Patch(url, entityJsonBody);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        // this method does not serialise an existing object and therefore does not use defined create/update safety, i.e. ShouldSerialize functions)
        public void Save(string partialDataObject)
        {
            if (!HasCrud()) throw new EntityException("Save operation not supported on object.");

            var url = GetEndpoint(true);
            var responseText = _connection.Patch(url, partialDataObject);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        public T Retrieve(long id)
        {
            return Retrieve(id.ToString());
        }

        public T Retrieve(string id = null)
        {
            var url = GetEndpoint(false);

            if (id != null) url += "/" + id;

            var responseText = _connection.Get(url, null);
            T serializedObject;
            try
            {
                serializedObject = JsonConvert.DeserializeObject<T>(responseText,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    });
                serializedObject._connection = _connection;
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            return serializedObject;
        }

        public virtual void Delete()
        {
            if (!HasCrud()) throw new EntityException("Delete operation not supported on object.");

            _connection.Delete(GetEndpoint(true));
        }

        public EntityList<T> List(string nextUrl, Dictionary<string, object> queryParams,
            JsonConverter customConverter = null)
        {
            if (!HasList()) throw new EntityException("List operation not supported on object.");

            var url = GetEndpoint(false);

            if (!string.IsNullOrEmpty(nextUrl)) url = nextUrl;

            var response = _connection.GetList(url, queryParams);

            EntityList<T> entities;

            var config = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            try
            {
                if (customConverter != null) config.Converters.Add(customConverter);
                entities = JsonConvert.DeserializeObject<EntityList<T>>(response.Result, config);

                entities.LinkURLS = response.Links;
                entities.TotalCount = response.TotalCount;
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }

            foreach (var entity in entities) entity.ChangeConnection(_connection);

            return entities;
        }

        public EntityList<T> ListAll(Dictionary<string, object> queryParams, JsonConverter customConverter = null)
        {
            return ListAll("", queryParams, customConverter);
        }

        public EntityList<T> ListAll(string nextUrl = "", Dictionary<string, object> queryParams = null,
            JsonConverter customConverter = null)
        {
            EntityList<T> entities = null;

            if (!HasList()) throw new EntityException("List operation not supported on object.");

            do
            {
                var tmpEntities = List(nextUrl, queryParams, customConverter);

                if (entities == null)
                {
                    entities = tmpEntities;
                    if (tmpEntities.TotalCount > 0)
                        entities.Capacity = tmpEntities.TotalCount;
                }
                else
                {
                    entities.AddRange(tmpEntities);
                    entities.LinkURLS = tmpEntities.LinkURLS;
                    entities.TotalCount = tmpEntities.TotalCount;
                }

                nextUrl = tmpEntities.GetNextURL();

            } while (!string.IsNullOrEmpty(entities.GetNextURL()) && entities.GetSelfURL() != entities.GetLastURL());

            return entities;
        }

        protected string ToJsonString([CallerMemberName] string enclosingFunction = "")
        {
            if (!string.IsNullOrEmpty(enclosingFunction)) CurrentOperation = enclosingFunction;

            var output = JsonConvert.SerializeObject(this, Formatting.Indented,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });

            CurrentOperation = null;
            return output;
        }

        public void Void()
        {
            if (!HasVoid()) throw new EntityException("Void operation not supported on object.");

            var url = GetEndpoint(true) + "/void";

            var responseText = _connection.Post(url, null, null);

            try
            {
                JsonConvert.PopulateObject(responseText, this);
            }
            catch (Exception e)
            {
                throw new EntityException("", e);
            }
        }

        public IList<Attachment> ListAttachments()
        {
            if (!HasAttachments()) throw new EntityException("List attachments operation not supported on object.");

            var url = GetEndpoint(true) + "/attachments";

            var responseText = _connection.Get(url, null);
            return JsonConvert.DeserializeObject<IList<Attachment>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public IList<Email> SendEmail(EmailRequest emailRequest)
        {
            if (!HasSends()) throw new EntityException("Send email operation not supported on object.");

            var url = GetEndpoint(true) + "/emails";

            var jsonRequestBody = emailRequest.ToJsonString();

            var responseText = _connection.Post(url, null, jsonRequestBody);
            return JsonConvert.DeserializeObject<IList<Email>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public Letter SendLetter(LetterRequest letterRequest = null)
        {
            if (!HasSends()) throw new EntityException("Send letter operation not supported on object.");

            string responseText;

            var url = GetEndpoint(true) + "/letters";

            if (letterRequest != null)
            {
                var jsonRequestBody = letterRequest.ToJsonString();
                responseText = _connection.Post(url, null, jsonRequestBody);
            }
            else
            {
                responseText = _connection.Post(url, null, "");
            }

            return JsonConvert.DeserializeObject<Letter>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public IList<TextMessage> SendText(TextRequest textRequest)
        {
            if (!HasSends()) throw new EntityException("Send text message operation not supported on object.");

            var url = GetEndpoint(true) + "/text_messages";

            var jsonRequestBody = textRequest.ToJsonString();

            var responseText = _connection.Post(url, null, jsonRequestBody);
            return JsonConvert.DeserializeObject<IList<TextMessage>>(responseText,
                new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        protected abstract string EntityId();

        protected virtual bool HasCrud()
        {
            return true;
        }

        protected virtual bool HasList()
        {
            return true;
        }

        protected virtual bool HasVoid()
        {
            return false;
        }

        protected virtual bool HasAttachments()
        {
            return false;
        }

        protected virtual bool HasSends()
        {
            return false;
        }
    }
}
