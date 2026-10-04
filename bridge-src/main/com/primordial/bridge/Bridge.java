/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  com.codex.forgelogin.AuthHooks
 *  com.google.gson.JsonObject
 *  com.google.gson.JsonParser
 *  net.minecraft.network.RegistryFriendlyByteBuf
 *  net.minecraft.network.chat.Component
 *  net.minecraft.network.codec.StreamCodec
 *  net.minecraft.network.protocol.common.custom.CustomPacketPayload
 *  net.minecraft.network.protocol.common.custom.CustomPacketPayload$Type
 *  net.minecraft.resources.ResourceLocation
 *  net.minecraft.server.MinecraftServer
 *  net.minecraft.server.level.ServerPlayer
 *  net.neoforged.api.distmarker.Dist
 *  net.neoforged.bus.api.IEventBus
 *  net.neoforged.bus.api.SubscribeEvent
 *  net.neoforged.fml.common.Mod
 *  net.neoforged.fml.loading.FMLEnvironment
 *  net.neoforged.neoforge.common.NeoForge
 *  net.neoforged.neoforge.event.entity.player.PlayerEvent$PlayerLoggedInEvent
 *  net.neoforged.neoforge.event.entity.player.PlayerEvent$PlayerLoggedOutEvent
 *  net.neoforged.neoforge.event.server.ServerStartedEvent
 *  net.neoforged.neoforge.event.server.ServerStoppedEvent
 *  net.neoforged.neoforge.network.PacketDistributor
 *  net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent
 */
package com.primordial.bridge;

import com.codex.forgelogin.AuthHooks;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.primordial.bridge.AuthCore;
import com.primordial.bridge.ClientBridge;
import com.primordial.bridge.SkinStore;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import net.minecraft.network.RegistryFriendlyByteBuf;
import net.minecraft.network.chat.Component;
import net.minecraft.network.codec.StreamCodec;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.neoforged.api.distmarker.Dist;
import net.neoforged.bus.api.IEventBus;
import net.neoforged.bus.api.SubscribeEvent;
import net.neoforged.fml.common.Mod;
import net.neoforged.fml.loading.FMLEnvironment;
import net.neoforged.neoforge.common.NeoForge;
import net.neoforged.neoforge.event.entity.player.PlayerEvent;
import net.neoforged.neoforge.event.server.ServerStartedEvent;
import net.neoforged.neoforge.event.server.ServerStoppedEvent;
import net.neoforged.neoforge.network.PacketDistributor;
import net.neoforged.neoforge.network.event.RegisterPayloadHandlersEvent;

@Mod(value="primordial_bridge")
public final class Bridge {
    public static final AuthCore CORE = new AuthCore();
    private static final Map<UUID, String> challenges = new HashMap<UUID, String>();
    private MinecraftServer server;
    private SkinStore skins;
    private HttpServer http;
    private ExecutorService httpPool;

    public Bridge(IEventBus iEventBus) {
        iEventBus.addListener(this::payloads);
        NeoForge.EVENT_BUS.register((Object)this);
        if (FMLEnvironment.dist == Dist.CLIENT) {
            ClientBridge.register();
        }
    }

    private void payloads(RegisterPayloadHandlersEvent registerPayloadHandlersEvent) {
        registerPayloadHandlersEvent.registrar("1").optional().playBidirectional(Message.TYPE, Message.CODEC, (message, iPayloadContext) -> {
            Object object = iPayloadContext.player();
            if (object instanceof ServerPlayer) {
                ServerPlayer serverPlayer = (ServerPlayer)object;
                if (!message.action.equals("proof")) {
                    return;
                }
                object = challenges.get(serverPlayer.getUUID());
                if (object == null) {
                    return;
                }
                try {
                    String string = CORE.skin(message.id);
                    if (!AuthHooks.whitelisted((MinecraftServer)serverPlayer.server, (String)serverPlayer.getGameProfile().getName()) || !CORE.consume(message.id, serverPlayer.getGameProfile().getName(), (String)object, serverPlayer.getUUID(), message.value)) {
                        PacketDistributor.sendToPlayer((ServerPlayer)serverPlayer, (CustomPacketPayload)new Message("error", "", "Automatic login expired or failed. Relaunch from Primordial Launcher."), (CustomPacketPayload[])new CustomPacketPayload[0]);
                        return;
                    }
                    challenges.remove(serverPlayer.getUUID());
                    AuthHooks.unlock((ServerPlayer)serverPlayer);
                    this.skins.set(serverPlayer.getUUID(), string);
                    for (ServerPlayer serverPlayer2 : serverPlayer.server.getPlayerList().getPlayers()) {
                        if (serverPlayer.connection.hasChannel(Message.TYPE)) {
                            PacketDistributor.sendToPlayer((ServerPlayer)serverPlayer, (CustomPacketPayload)new Message("skin", serverPlayer2.getUUID().toString(), this.skins.get(serverPlayer2.getUUID())), (CustomPacketPayload[])new CustomPacketPayload[0]);
                        }
                        if (!serverPlayer2.connection.hasChannel(Message.TYPE)) continue;
                        PacketDistributor.sendToPlayer((ServerPlayer)serverPlayer2, (CustomPacketPayload)new Message("skin", serverPlayer.getUUID().toString(), string), (CustomPacketPayload[])new CustomPacketPayload[0]);
                    }
                    PacketDistributor.sendToPlayer((ServerPlayer)serverPlayer, (CustomPacketPayload)new Message("ok", "", "Logged in automatically."), (CustomPacketPayload[])new CustomPacketPayload[0]);
                }
                catch (Exception exception) {
                    serverPlayer.connection.disconnect((Component)Component.literal((String)"Authentication integration unavailable. Contact the host."));
                }
            } else if (FMLEnvironment.dist == Dist.CLIENT) {
                ClientBridge.receive(message);
            }
        });
    }

    @SubscribeEvent
    public void joined(PlayerEvent.PlayerLoggedInEvent playerLoggedInEvent) {
        Object object = playerLoggedInEvent.getEntity();
        if (object instanceof ServerPlayer) {
            ServerPlayer serverPlayer = (ServerPlayer)object;
            object = CORE.nonce();
            challenges.put(serverPlayer.getUUID(), (String)object);
            if (serverPlayer.connection.hasChannel(Message.TYPE)) {
                PacketDistributor.sendToPlayer((ServerPlayer)serverPlayer, (CustomPacketPayload)new Message("challenge", serverPlayer.getUUID().toString(), (String)object), (CustomPacketPayload[])new CustomPacketPayload[0]);
            }
        }
    }

    @SubscribeEvent
    public void left(PlayerEvent.PlayerLoggedOutEvent playerLoggedOutEvent) {
        challenges.remove(playerLoggedOutEvent.getEntity().getUUID());
    }

    @SubscribeEvent
    public void started(ServerStartedEvent serverStartedEvent) throws IOException {
        this.server = serverStartedEvent.getServer();
        this.skins = new SkinStore();
        this.http = HttpServer.create(new InetSocketAddress("127.0.0.1", 8081), 16);
        this.httpPool = Executors.newFixedThreadPool(4);
        this.http.setExecutor(this.httpPool);
        this.http.createContext("/api/auth/", this::request);
        this.http.start();
    }

    @SubscribeEvent
    public void stopped(ServerStoppedEvent serverStoppedEvent) {
        if (this.http != null) {
            this.http.stop(0);
        }
        if (this.httpPool != null) {
            this.httpPool.shutdownNow();
        }
        challenges.clear();
    }

    private static <T> T unchecked(Callable<T> callable) {
        try {
            return callable.call();
        }
        catch (Exception exception) {
            throw new IllegalStateException("Authentication unavailable", exception);
        }
    }

    /*
     * WARNING - Removed try catching itself - possible behaviour change.
     */
    private void request(HttpExchange httpExchange) throws IOException {
        try {
            String string;
            boolean bl;
            if (!httpExchange.getRequestMethod().equals("POST")) {
                Bridge.reply(httpExchange, 405, "method_not_allowed");
                return;
            }
            if (!CORE.allow("global", 120)) {
                Bridge.reply(httpExchange, 429, "try_later");
                return;
            }
            byte[] byArray = httpExchange.getRequestBody().readNBytes(4097);
            if (byArray.length > 4096) {
                Bridge.reply(httpExchange, 413, "too_large");
                return;
            }
            JsonObject jsonObject = JsonParser.parseString((String)new String(byArray, StandardCharsets.UTF_8)).getAsJsonObject();
            String string2 = jsonObject.get("name").getAsString();
            if (!string2.matches("[A-Za-z0-9_]{3,16}")) {
                Bridge.reply(httpExchange, 400, "invalid_name");
                return;
            }
            boolean bl2 = (Boolean)this.server.submit(() -> AuthHooks.whitelisted((MinecraftServer)this.server, (String)string2)).get(5L, TimeUnit.SECONDS);
            if (!bl2) {
                Bridge.reply(httpExchange, 403, "not_whitelisted");
                return;
            }
            if (httpExchange.getRequestURI().getPath().equals("/api/auth/status")) {
                boolean bl3 = (Boolean)this.server.submit(() -> Bridge.unchecked(() -> AuthHooks.registered((String)string2))).get(5L, TimeUnit.SECONDS);
                JsonObject jsonObject2 = new JsonObject();
                jsonObject2.addProperty("status", bl3 ? "claimed" : "unclaimed");
                Bridge.reply(httpExchange, 200, jsonObject2);
                return;
            }
            if (!httpExchange.getRequestURI().getPath().equals("/api/auth/session")) {
                Bridge.reply(httpExchange, 404, "not_found");
                return;
            }
            if (!CORE.allow("name:" + string2.toLowerCase(Locale.ROOT), 6)) {
                Bridge.reply(httpExchange, 429, "try_later");
                return;
            }
            String string3 = jsonObject.get("password").getAsString();
            final boolean create = jsonObject.has("register") && jsonObject.get("register").getAsBoolean();
            string = jsonObject.has("skin") ? jsonObject.get("skin").getAsString() : "default";
            if (!SkinStore.IDS.contains(string)) { Bridge.reply(httpExchange, 400, "unknown_skin"); return; }
            String string4 = (String)this.server.submit(() -> this.lambda$request$5(string2, string3, create)).get(10L, TimeUnit.SECONDS);
            if (!string4.equals("ok")) {
                Bridge.reply(httpExchange, string4.equals("storage_error") ? 503 : 403, string4);
                return;
            }
            String string5 = string = jsonObject.has("skin") ? jsonObject.get("skin").getAsString() : "default";
            if (!SkinStore.IDS.contains(string)) {
                Bridge.reply(httpExchange, 400, "unknown_skin");
                return;
            }
            AuthCore.Ticket ticket = CORE.issue(string2, string);
            JsonObject jsonObject3 = new JsonObject();
            jsonObject3.addProperty("status", "ok");
            jsonObject3.addProperty("id", ticket.id());
            jsonObject3.addProperty("name", ticket.name());
            jsonObject3.addProperty("secret", ticket.secret());
            jsonObject3.addProperty("expires", (Number)ticket.expires());
            Bridge.reply(httpExchange, 200, jsonObject3);
        }
        catch (Exception exception) {
            Bridge.reply(httpExchange, 400, "request_failed");
        }
        finally {
            httpExchange.close();
        }
    }

    private static void reply(HttpExchange httpExchange, int n, String string) throws IOException {
        JsonObject jsonObject = new JsonObject();
        jsonObject.addProperty("status", string);
        Bridge.reply(httpExchange, n, jsonObject);
    }

    private static void reply(HttpExchange httpExchange, int n, JsonObject jsonObject) throws IOException {
        byte[] byArray = jsonObject.toString().getBytes(StandardCharsets.UTF_8);
        httpExchange.getResponseHeaders().set("Content-Type", "application/json");
        httpExchange.getResponseHeaders().set("Cache-Control", "no-store");
        httpExchange.sendResponseHeaders(n, byArray.length);
        httpExchange.getResponseBody().write(byArray);
    }

    private /* synthetic */ String lambda$request$5(String string, String string2, boolean bl) {
        return Bridge.unchecked(() -> AuthHooks.authenticate((MinecraftServer)this.server, (String)string, (String)string2, (boolean)bl));
    }

    public record Message(String action, String id, String value) implements CustomPacketPayload
    {
        public static final CustomPacketPayload.Type<Message> TYPE = new CustomPacketPayload.Type(ResourceLocation.fromNamespaceAndPath((String)"primordial_bridge", (String)"session"));
        public static final StreamCodec<RegistryFriendlyByteBuf, Message> CODEC = StreamCodec.of((registryFriendlyByteBuf, message) -> {
            registryFriendlyByteBuf.writeUtf(message.action, 32);
            registryFriendlyByteBuf.writeUtf(message.id, 128);
            registryFriendlyByteBuf.writeUtf(message.value, 4096);
        }, registryFriendlyByteBuf -> new Message(registryFriendlyByteBuf.readUtf(32), registryFriendlyByteBuf.readUtf(128), registryFriendlyByteBuf.readUtf(4096)));

        public CustomPacketPayload.Type<Message> type() {
            return TYPE;
        }
    }
}
