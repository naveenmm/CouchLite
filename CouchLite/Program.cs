using System;
using Couchbase;
using Couchbase.Configuration.Client;
using System.Collections.Generic;
using Couchbase.Authentication;
using Couchbase.Core;
using System.Linq;
using Newtonsoft.Json;
using Couchbase.N1QL;
using Couchbase.Lite.Storage.SystemSQLite;
using Couchbase.Lite;
using Couchbase.Lite.Query;
using Couchbase.Lite.Sync;
using System.IO;

namespace CouchLite
{
    class Program
    {
        static void Main(string[] args)
        {

            // Get the database (and create it if it doesn't exist)
            var database = new Database("mydb");
            // Create a new document (i.e. a record) in the database
            string id = null;
            using (var mutableDoc = new MutableDocument())
            {
                mutableDoc.SetFloat("version", 2.0f)
                    .SetString("type", "SDK");

                // Save it to the database
                database.Save(mutableDoc);
                id = mutableDoc.Id;
            }

            // Update a document
            using (var doc = database.GetDocument(id))
            using (var mutableDoc = doc.ToMutable())
            {
                mutableDoc.SetString("language", "python");
                database.Save(mutableDoc);

                using (var docAgain = database.GetDocument(id))
                {
                    Console.WriteLine($"Document ID :: {docAgain.Id}");
                    Console.WriteLine($"Learning {docAgain.GetString("language")}");
                }
            }

            // Create a query to fetch documents of type SDK
            // i.e. SELECT * FROM database WHERE type = "SDK"
            using (var query = QueryBuilder.Select(SelectResult.All())
                .From(DataSource.Database(database))
                .Where(Expression.Property("type").EqualTo(Expression.String("SDK"))))
            {
                // Run the query
                var result = query.Execute();
                Console.WriteLine($"Number of rows :: {result.Count()}");
               
            }


            using (var query = QueryBuilder.Select(
        SelectResult.Expression(Meta.ID),
        SelectResult.Property("language"))
    .From(DataSource.Database(database)))
            {
                foreach (var result in query.Execute())
                {
                    Console.WriteLine($"Document Name :: {result.GetString("language")}");
                }
            }

            // Create replicator to push and pull changes to and from the cloud
            var targetEndpoint = new URLEndpoint(new Uri("ws://localhost:4984/getting-started-db"));
            var replConfig = new ReplicatorConfiguration(database, targetEndpoint);

            // Add authentication
            replConfig.Authenticator = new BasicAuthenticator("john", "pass");

            // Create replicator (make sure to add an instance or static variable
            // named _Replicator)
            var _Replicator = new Replicator(replConfig);
            _Replicator.AddChangeListener((sender, args) =>
            {
                if (args.Status.Error != null)
                {
                    Console.WriteLine($"Error :: {args.Status.Error}");
                }
            });
            //Path.Combine(AppContext.BaseDirectory, "CouchbaseLite");
            _Replicator.Start();
        }
    }
}
