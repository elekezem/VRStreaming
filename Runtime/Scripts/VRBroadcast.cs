﻿/**
 * Copyright 2021 Vasanth Mohan. All rights and licenses reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.RenderStreaming;

namespace FusedVR.VRStreaming {
    /// <summary>
    /// VRBroadcast is very similar to the Broadcast script included with Render Streaming.
    /// This script is responsible for listening for offers from the client and choosing when to respond. 
    /// </summary>
    public class VRBroadcast : SignalingHandlerBase,
        IOfferHandler, IAddChannelHandler, IDisconnectHandler, IDeletedConnectionHandler {

        /// <summary>
        /// Streams (video, audio, data) that need to be sent to the client
        /// </summary>
        [SerializeField]
        private List<Component> streams = new List<Component>();

        /// <summary>
        /// List of all connectionIds that are connected
        /// </summary>
        private List<string> connectionIds = new List<string>();

        #region Disconnect
        public void OnDeletedConnection(SignalingEventData eventData) {
            Disconnect(eventData.connectionId);
        }

        public void OnDisconnect(SignalingEventData eventData) {
            Disconnect(eventData.connectionId);
        }

        private void Disconnect(string connectionId) {
            if (!connectionIds.Contains(connectionId))
                return;
            connectionIds.Remove(connectionId);

            foreach (var source in streams.OfType<IStreamSource>()) {
                source.SetSender(connectionId, null);
            }
            foreach (var receiver in streams.OfType<IStreamReceiver>()) {
                receiver.SetReceiver(connectionId, null);
            }
            foreach (var channel in streams.OfType<IDataChannel>()) {
                channel.SetChannel(connectionId, null);
            }
        }
        #endregion

        /// <summary>
        /// Event that is called when an Offer is made by a client
        /// Determines whether to accept offer and if so that to apply sources and submit answer
        /// </summary>
        public void OnOffer(SignalingEventData data) {
            if (connectionIds.Count >= 1) { //if there is more than 1 connection, let's skip this offer
                Debug.Log($"Already answered this connectionId : {connectionIds[0]}");
                return;
            }

            if (connectionIds.Contains(data.connectionId)) { //if connection is already connected, skip offer
                Debug.Log($"Already answered this connectionId : {data.connectionId}");
                return;
            }
            connectionIds.Add(data.connectionId); //confirm we will use this connection

            foreach (var source in streams.OfType<IStreamSource>()) {
                var transceiver = AddTrack(data.connectionId, source.Track);
                source.SetSender(data.connectionId, transceiver.Sender);
            }
            foreach (var channel in streams.OfType<IDataChannel>().Where(c => c.IsLocal)) {
                var _channel = CreateChannel(data.connectionId, channel.Label);
                channel.SetChannel(data.connectionId, _channel);
            }
            SendAnswer(data.connectionId); //accept offer with an answer
        }

        /// <summary>
        /// Apply Data Channel
        /// </summary>
        public void OnAddChannel(SignalingEventData data) {
            var channel = streams.OfType<IDataChannel>().
                FirstOrDefault(r => r.Channel == null && !r.IsLocal);
            channel?.SetChannel(data.connectionId, data.channel);
        }
    }
}
