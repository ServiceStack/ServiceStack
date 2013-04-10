// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceStack.Html.AntiXsrf
{
    internal sealed class AntiForgeryTokenSerializer : IAntiForgeryTokenSerializer
    {
        private const byte TokenVersion = 0x01;
        private readonly ICryptoSystem _cryptoSystem;

        internal AntiForgeryTokenSerializer(ICryptoSystem cryptoSystem)
        {
            _cryptoSystem = cryptoSystem;
        }

        public AntiForgeryToken Deserialize(string serializedToken)
        {
            try {
                using (MemoryStream stream = new MemoryStream(_cryptoSystem.Unprotect(serializedToken))) {
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        AntiForgeryToken token = DeserializeImpl(reader);
                        if (token != null) {
                            return token;
                        }
                    }
                }
            } catch {
                // swallow all exceptions - homogenize error if something went wrong
            }

            // if we reached this point, something went wrong deserializing
            throw HttpAntiForgeryException.CreateDeserializationFailedException();
        }

        /* The serialized format of the anti-XSRF token is as follows:
         * Version: 1 byte integer
         * SecurityToken: 16 byte binary blob
         * IsSessionToken: 1 byte Boolean
         * [if IsSessionToken = true]
         *   +- IsClaimsBased: 1 byte Boolean
         *   |  [if IsClaimsBased = true]
         *   |    `- ClaimUid: 32 byte binary blob
         *   |  [if IsClaimsBased = false]
         *   |    `- Username: UTF-8 string with 7-bit integer length prefix
         *   `- AdditionalData: UTF-8 string with 7-bit integer length prefix
         */
        private static AntiForgeryToken DeserializeImpl(BinaryReader reader)
        {
            // we can only consume tokens of the same serialized version that we generate
            byte embeddedVersion = reader.ReadByte();
            if (embeddedVersion != TokenVersion) {
                return null;
            }

            AntiForgeryToken deserializedToken = new AntiForgeryToken();
            byte[] securityTokenBytes = reader.ReadBytes(AntiForgeryToken.SecurityTokenBitLength / 8);
            deserializedToken.SecurityToken = new BinaryBlob(AntiForgeryToken.SecurityTokenBitLength, securityTokenBytes);
            deserializedToken.IsSessionToken = reader.ReadBoolean();

            if (!deserializedToken.IsSessionToken) {
                bool isClaimsBased = reader.ReadBoolean();
                if (isClaimsBased) {
                    byte[] claimUidBytes = reader.ReadBytes(AntiForgeryToken.ClaimUidBitLength / 8);
                    deserializedToken.ClaimUid = new BinaryBlob(AntiForgeryToken.ClaimUidBitLength, claimUidBytes);
                } else {
                    deserializedToken.Username = reader.ReadString();
                }

                deserializedToken.AdditionalData = reader.ReadString();
            }

            // if there's still unconsumed data in the stream, fail
            if (reader.BaseStream.ReadByte() != -1) {
                return null;
            }

            // success
            return deserializedToken;
        }

        public string Serialize(AntiForgeryToken token)
        {
#if NET_4_0
            Contract.Assert(token != null);
#endif
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(TokenVersion);
                    writer.Write(token.SecurityToken.GetData());
                    writer.Write(token.IsSessionToken);

                    if (!token.IsSessionToken) {
                        if (token.ClaimUid != null) {
                            writer.Write(true /* isClaimsBased */);
                            writer.Write(token.ClaimUid.GetData());
                        } else {
                            writer.Write(false /* isClaimsBased */);
                            writer.Write(token.Username);
                        }

                        writer.Write(token.AdditionalData);
                    }

                    writer.Flush();
                    return _cryptoSystem.Protect(stream.ToArray());
                }
            }
        }
    }
}
