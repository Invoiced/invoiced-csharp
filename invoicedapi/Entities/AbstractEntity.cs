using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Reflection;

namespace Invoiced
{

    public abstract class AbstractEntity<T> where T : AbstractEntity<T> {

		protected Connection Connection;
		private bool _entityCreated;

		// used to determine safe json serialisation. should always be null outside function bodies
		protected string CurrentOperation;

		public bool ShouldSerializeCurrentOperation() {
			return false;
		}
		
		public override string ToString() {
			var s = base.ToString() + "<" + this.EntityIdString() +">";
			var jsonS =  s + " " + this.ToJsonString();

			return jsonS;
		}

		internal AbstractEntity(Connection conn) {
			this.Connection = conn;

		}

		protected AbstractEntity() {

		}

		protected Connection GetConnection() {
			return this.Connection;
		}

		public void ChangeConnection(Connection conn) {
			this.Connection = conn;
		}

		public void Create() {

			if (this._entityCreated) {
				return;
			}

			if (!this.HasCRUD()) {
				return;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName();
			string entityJsonBody = this.ToJsonString();
			string responseText = this.Connection.Post(url,null,entityJsonBody);
		
			try {
				JsonConvert.PopulateObject(responseText,this);
			} catch(Exception e) {
				throw new EntityException("",e);
			}

			this._entityCreated = true;

		}

		// this method serialises the existing object (with respect for defined create/update safety, i.e. ShouldSerialize functions)
		public void SaveAll() {

			if (!this.HasCRUD()) {
				return;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString();
			string entityJsonBody = this.ToJsonString();
			string responseText = this.Connection.Patch(url,entityJsonBody);
			
			try {
				JsonConvert.PopulateObject(responseText,this);
			} catch(Exception e) {
				throw new EntityException("",e);
			}


		}

		// this method does not serialise an existing object
		public void Save(string partialDataObject) {

			if (!this.HasCRUD()) {
				return;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString();
			string responseText = this.Connection.Patch(url,partialDataObject);
			
			try {
				JsonConvert.PopulateObject(responseText,this);
			} catch(Exception e) {
				throw new EntityException("",e);
			}

		}

		public T Retrieve(long id) {

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + id.ToString();
			string responseText = this.Connection.Get(url,null);
			T serializedObject;
			try {
					serializedObject = JsonConvert.DeserializeObject<T>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
					serializedObject.Connection = this.Connection;
			} catch(Exception e) {
				throw new EntityException("",e);
			}

			return serializedObject;
			
		}

		public T Retrieve() {

			string url = this.Connection.baseUrl() + "/" + this.EntityName();
			string responseText = this.Connection.Get(url,null);
			T serializedObject;
			try {
					serializedObject = JsonConvert.DeserializeObject<T>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
					serializedObject.Connection = this.Connection;
			} catch(Exception e) {
				throw new EntityException("",e);
			}

			return serializedObject;
			
		}

		public T Retrieve(string id) {

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + id;
			string responseText = this.Connection.Get(url,null);
			T serializedObject;
			try {
					serializedObject = JsonConvert.DeserializeObject<T>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
					serializedObject.Connection = this.Connection;
			} catch(Exception e) {
				throw new EntityException("",e);
			}

			return serializedObject;
			
		}

		public virtual void Delete() {

			if (!HasCRUD()) {
				return;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString();
			
			this.Connection.Delete(url);

		}

		private EntityList<T> List(string nextUrl,Dictionary<string,Object> queryParams) {

			if (!this.HasList()) {
				return null;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName();
			
			if (!string.IsNullOrEmpty(nextUrl)) {
				url = nextUrl;
			}

			ListResponse response = this.Connection.GetList(url,queryParams);

			EntityList<T> entities;
			
			try {
					entities = JsonConvert.DeserializeObject<EntityList<T>>(response.Result,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });
					entities.LinkURLS = response.Links;
					entities.TotalCount = response.TotalCount;
			} catch(Exception e) {
				throw new EntityException("",e);
			}

			foreach (var entity in entities) {
				entity.ChangeConnection(Connection);
			}

			return entities;

		}

		public EntityList<T> ListAll(Dictionary<string,Object> queryParams) {
				var entities = ListAll("",queryParams);
				return entities;
		}

		public EntityList<T> ListAll(string nextUrl,Dictionary<string,Object> queryParams) {

			EntityList<T> entities = null;

			if (!this.HasList()) {
				return null;
			}

			var tmpEntities = this.List(nextUrl,queryParams);

			do {
				if (entities == null) {
					entities = tmpEntities;
				} else {
					entities.AddRange(tmpEntities);
					entities.LinkURLS = tmpEntities.LinkURLS;
					entities.TotalCount = tmpEntities.TotalCount;
				}

			} while((!string.IsNullOrEmpty(entities.GetNextURL()) && (entities.GetSelfURL() != entities.GetLastURL())));

			return entities;

		}

		protected string ToJsonString([System.Runtime.CompilerServices.CallerMemberName] string enclosingFunction = "") {
			if (enclosingFunction != "") {
				this.CurrentOperation = enclosingFunction;
			}

			var output = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore } );

			this.CurrentOperation = null;
			return output;
		}

		public void Void() {

			if (!this.HasVoid()) {
				return;
			}

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString() + "/void";

			string responseText = this.Connection.Post(url,null,null);
			
			try {
				JsonConvert.PopulateObject(responseText,this);
			} catch(Exception e) {
				throw new EntityException("",e);
			}
		}

		public IList<Attachment> ListAttachments() {

			if (!this.HasAttachments()) {
				return null;
			}

			IList<Attachment> objects = null;

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString() + "/attachments";

			string responseText = this.Connection.Get(url,null);
			objects = JsonConvert.DeserializeObject<IList<Attachment>>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });

			return objects;
		}

		public IList<Email> SendEmail(EmailRequest emailRequest) {

			if (!this.HasSends()) {
				return null;
			}

			IList<Email> objects = null;

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString() + "/emails";

			string jsonRequestBody = emailRequest.ToJsonString();

			string responseText = this.Connection.Post(url,null,jsonRequestBody);
			objects = JsonConvert.DeserializeObject<IList<Email>>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });

			return objects;
		}

		public Letter SendLetter(LetterRequest letterRequest = null) {

			if (!this.HasSends()) {
				return null;
			}

			Letter letter = null;
			string responseText = null;

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString() + "/letters";

			if (letterRequest != null) {
				string jsonRequestBody = letterRequest.ToJsonString();
				responseText = this.Connection.Post(url, null, jsonRequestBody);
			} else {
				responseText = this.Connection.Post(url, null, "");
			}

			letter = JsonConvert.DeserializeObject<Letter>(responseText,
				new JsonSerializerSettings
					{NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore});

				return letter;
		}

		public IList<TextMessage> SendText(TextRequest textRequest) {

			if (!this.HasSends()) {
				return null;
			}

			IList<TextMessage> objects = null;

			string url = this.Connection.baseUrl() + "/" + this.EntityName() + "/" + this.EntityIdString() + "/text_messages";

			string jsonRequestBody = textRequest.ToJsonString();

			string responseText = this.Connection.Post(url,null,jsonRequestBody);
			objects = JsonConvert.DeserializeObject<IList<TextMessage>>(responseText,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore });

			return objects;
		}

		protected abstract string EntityIdString();
		public abstract string EntityName();

		public virtual bool HasCRUD() {
			return true;
		}

		public virtual bool HasList() {
			return true;
		}

		public virtual bool HasVoid() {
			return false;
		}
		
		public virtual bool HasStringId() {
			return false;
		}

		public virtual bool HasAttachments() {
			return false;
		}

		public virtual bool HasSends() {
			return false;
		}

	}

}
