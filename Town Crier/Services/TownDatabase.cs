using Alta.WebApi.Models;
using Alta.WebApi.Utility;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Discord;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Town_Crier.Database;
using TownCrier.Database;

namespace TownCrier.Services
{
    public interface ITableAccess<T>
    {
        string Name { get; }
        int Count();
        //int Count(Expression<Func<T, bool>> predicate);
        //int Count(Query query);
        //int Delete(Query query);
        //int Delete(Expression<Func<T, bool>> predicate);
        //bool Delete(BsonValue id);
        //bool DropIndex(string field);
        //bool EnsureIndex<K>(Expression<Func<T, K>> property, string expression, bool unique = false);
        //bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false);
        //bool EnsureIndex(string field, string expression, bool unique = false);
        //bool EnsureIndex(string field, bool unique = false);
        //bool Exists(Query query);
        //bool Exists(Expression<Func<T, bool>> predicate);
        //IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
        //IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);
        IEnumerable<T> FindAll();
        T FindOne();
        T FindById(ulong id);
        T FindByIndex(object id, string index, string fieldName);
        IEnumerable<T> FindAllByIndex(object id, string index, string fieldName);
        //T FindOne(Query query);
        //T FindOne(Expression<Func<T, bool>> predicate);
        //IEnumerable<IndexInfo> GetIndexes();
        //LiteCollection<T> Include(string path);
        //LiteCollection<T> Include(string[] paths);
        //LiteCollection<T> Include<K>(Expression<Func<T, K>> path);
        //LiteCollection<T> IncludeAll(int maxDepth = -1);
        //void Insert(BsonValue id, T document);
        //int Insert(IEnumerable<T> docs);
        void Insert(T document);
        //int InsertBulk(IEnumerable<T> docs, int batchSize = 5000);
        //long LongCount(Query query);
        //long LongCount(Expression<Func<T, bool>> predicate);
        //long LongCount();
        //BsonValue Max(string field);
        //BsonValue Max();
        //BsonValue Max<K>(Expression<Func<T, K>> property);
        //BsonValue Min();
        //BsonValue Min<K>(Expression<Func<T, K>> property);
        //BsonValue Min(string field);
        bool Update(T document);
        //bool Update(BsonValue id, T document);
        //int Update(IEnumerable<T> documents);
        //bool Upsert(BsonValue id, T document);
        //bool Upsert(T document);
        //int Upsert(IEnumerable<T> documents);
    }

    public class LiteDBTableAccess<T> : ITableAccess<T>
    {
        LiteDatabase Database { get; }

        public AltaAPI AltaApi { get; }

        ILiteCollection<T> Table { get; }

        public string Name { get; }

        public LiteDBTableAccess(AltaAPI altaApi, LiteDatabase database, string name)
        {
            Name = name;

            AltaApi = altaApi;

            Database = database;

            Table = database.GetCollection<T>(name);
        }

        public int Count()
        {
            return Table.Count();
        }

        public T FindOne()
        {
            return Table.FindOne(item => true);
        }

        public T FindById(ulong id)
        {
            return Table.FindById(new BsonValue((decimal)id));
        }

        public T FindByIndex(object value, string index, string fieldName)
        {
            return Table.FindOne(Query.EQ(fieldName, new BsonValue((int)value)));
        }

        public IEnumerable<T> FindAllByIndex(object value, string index, string fieldName)
        {
            return Table.Find(Query.EQ(fieldName, new BsonValue((int)value)));
        }

        public void Insert(T document)
        {
            Table.Insert(document);
        }

        public bool Update(T document)
        {
            return Table.Update(document);
        }

        public IEnumerable<T> FindAll()
        {
            List<BsonDocument> failed = new List<BsonDocument>();

            foreach (BsonDocument bson in Database.GetCollection(Name).FindAll())
            {
                T item = default(T);

                try
                {
                    item = BsonMapper.Global.ToObject<T>(bson);
                }
                catch (Exception e)
                {
                    failed.Add(bson);
                    continue;
                }

                yield return item;
            }

            Console.WriteLine(failed.Count);
        }
    }

    public class DynamoTableAccess<T> : ITableAccess<T>
        where T : class
    {
        public DynamoDBContext Context { get; }

        public Table Table { get; }

        public string Name => throw new NotImplementedException();

        public DynamoTableAccess(DynamoDBContext context)
        {
            Context = context;

            Table = Context.GetTargetTable<T>();
        }

        public int Count()
        {
            return Table.Scan(new ScanFilter()).Count;
        }

        public T FindOne()
        {
            Document result = Table.Scan(new ScanOperationConfig() { Limit = 1 }).GetRemainingAsync().Result.FirstOrDefault();

            if (result == null)
            {
                return null;
            }

            return Context.FromDocument<T>(result);
        }

        public T FindById(ulong id)
        {
            return Context.LoadAsync<T>(id).Result;
        }

        public T FindByIndex(object value, string index, string fieldName)
        {
            //Completely custom... Not caring what params are
            return Context.FromQueryAsync<T>(new QueryOperationConfig
            {
                IndexName = index,
                Limit = 1,
                Filter = new QueryFilter("alta_id", QueryOperator.Equal, (int)value)
            })
            .GetRemainingAsync().Result
            .FirstOrDefault();

            //return Context.Query<T>(value, new DynamoDBOperationConfig() { IndexName = index });
        }

        public IEnumerable<T> FindAllByIndex(object value, QueryOperator queryOperator, string index)
        {
            return Context.QueryAsync<T>(value, new DynamoDBOperationConfig() { IndexName = index }).GetRemainingAsync().Result;
        }

        public IEnumerable<T> FindAllByIndex(object value, string index, string fieldName)
        {
            return Context.QueryAsync<T>(value, new DynamoDBOperationConfig() { IndexName = index }).GetRemainingAsync().Result;
        }

        public void Insert(T document)
        {
            Context.SaveAsync<T>(document).Wait();
        }

        public bool Update(T document)
        {
            Context.SaveAsync<T>(document).Wait();

            return true;
        }

        public IEnumerable<T> FindAll()
        {
            return Context.ScanAsync<T>(new List<ScanCondition>()).GetRemainingAsync().Result;
        }
    }


    public class TownDatabase
    {
        LiteDatabase database;

        DynamoDBContext dynamo;
        public AltaAPI AltaApi { get; }

        public ITableAccess<TownGuild> Guilds { get; }
        public ITableAccess<TownUser> Users { get; }

        //public TownDatabase(AltaAPI altaApi, IAmazonDynamoDB dbContext, LiteDatabase database)
        //{
        //    this.database = database;
        //    this.AltaApi = altaApi;

        //    Guilds = new LiteDBTableAccess<TownGuild>(altaApi, database, "Guilds");
        //    Users = new LiteDBTableAccess<TownUser>(altaApi, database, "Users");

        //    Console.WriteLine("Migrating to dynamo!");
        //    Migrate(dbContext);

        //    Guilds = new DynamoTableAccess<TownGuild>(dynamo);
        //    Users = new DynamoTableAccess<TownUser>(dynamo);
        //}

        public TownDatabase(AltaAPI altaApi, IAmazonDynamoDB dbContext)
        {
            Console.WriteLine("Running DDB!");
            
            this.AltaApi = altaApi;

            dynamo = new DynamoDBContext(dbContext);

            Guilds = new DynamoTableAccess<TownGuild>(dynamo);
            Users = new DynamoTableAccess<TownUser>(dynamo);
        }

        public TownDatabase(AltaAPI altaApi, LiteDatabase database)
        {
            Console.WriteLine("Running LiteDB!");
            this.database = database;
            this.AltaApi = altaApi;

            Guilds = new LiteDBTableAccess<TownGuild>(altaApi, database, "Guilds");
            Users = new LiteDBTableAccess<TownUser>(altaApi, database, "Users");
        }

        //void Migrate(IAmazonDynamoDB dbContext)
        //{
        //    dynamo = new DynamoDBContext(dbContext);

        //    var newGuilds = new DynamoTableAccess<TownGuild>(dynamo);
        //    var newUsers = new DynamoTableAccess<TownUser>(dynamo);

        //    Migrate(Guilds, newGuilds);
        //    Migrate(Users, newUsers, FixUser);
        //}

        //void FixUser(TownUser user)
        //{
        //    if (user.AltaInfo != null)
        //    {
        //        user.AltaId = user.AltaInfo.Identifier;
        //    }
        //}

        void Migrate<T>(ITableAccess<T> from, ITableAccess<T> to, Action<T> modify = null)
        {
            T last;
            foreach (T item in from.FindAll())
            {
                last = item;
                modify?.Invoke(item);

                to.Insert(item);
            }
        }

        public TownGuild GetGuild(IGuild guild)
        {
            return Guilds.FindById(guild.Id);
        }

        public async Task<int> GetUserId(string linkedIdentifier)
        {
            try
            {
                var queryFilter = new QueryFilter();

                queryFilter.AddCondition("linked_id", QueryOperator.Equal, linkedIdentifier);
                queryFilter.AddCondition("service", QueryOperator.Equal, Service.Discord.ToString());

                var config = new QueryOperationConfig()
                {
                    Limit = 1,
                    Filter = queryFilter,
                    Select = SelectValues.AllProjectedAttributes,

                    IndexName = "linked_id-index"
                };

                var items = await dynamo.QueryAsync<DiscordAccount>(config).GetNextSetAsync();
                DiscordAccount account = items.FirstOrDefault();

                if (account == null)
                {
                    return -1;
                }

                return account.UserId;
            }
            catch (ItemNotFoundException e)
            {
                return -1;
            }
        }

        public async Task RefreshAltaUser(TownUser user)
        {
            int id = await GetUserId(user.UserId.ToString());

            if (user.AltaId != id)
			{
                user.AltaId = id;

                Users.Update(user);
			}
        }

        public TownUser GetUser(IUser user)
        {
            TownUser result = Users.FindById(user.Id);

            bool isChanged = false;

            if (result == null)
            {
                result = new TownUser() { UserId = user.Id, Name = user.Username };

                Users.Insert(result);
            }
            else if (result.Name != user.Username)
            {
                result.Name = user.Username;

                isChanged = true;
            }

            if (result.InitialJoin == default(DateTime) && user is IGuildUser guildUser && guildUser.JoinedAt.HasValue)
            {
                result.InitialJoin = guildUser.JoinedAt.Value.UtcDateTime;

                isChanged = true;
            }

            if (isChanged)
            {
                Users.Update(result);
            }

            return result;
        }
    }
}
