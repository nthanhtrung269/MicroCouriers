﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Tracking.Domain.AggregatesModel.TrackingAggregate;
using Tracking.Domain.Interfaces;
using Tracking.Persistence.Repositories;
using Tracking.ReadModel.Cache;

namespace Tracking.ReadModel.Database
{
    public class EventStore
    {
        private string _connectionString;
        private readonly ITrackingRepository _trackingContext;

        public EventStore()
        {             
            _connectionString = "Server=(localdb)\\mssqllocaldb;Database=TrackingEventStore;Trusted_Connection=True;";
            _trackingContext = new TrackingRepository(_connectionString);
        }

        public async Task<IEnumerable<string>> GetAllBookings()
        {
            RedisCacheService rCache = new RedisCacheService();

            IEnumerable<string> listId = new List<string>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try {

                    rCache.ClearAllKeys();

                    // get aggregate
                    listId = await conn
                           .QueryAsync<string>(
                               "select distinct id from TrackingEvent  where id in(select id from TrackingEvent group by id having sum(case when MessageType = 'OrderDelivered' then 1 else 0 end) = 0) ");

                    foreach (var item in listId)
                    {
                        var getva = rCache.Get(item);
                    }

                    foreach (var item in listId)
                    {                        
                        Track trackings = await _trackingContext.GetTrackingAsync(item);
                        var result = String.Join(", ", trackings.orderHistory.ToArray());
                        rCache.Save(item, result);
                    }

                    foreach (var item in listId)
                    {
                        var getva = rCache.Get(item);
                    }

                }
                catch(Exception ex)
                {

                }
                
            }
            return listId;
        }
    }
}