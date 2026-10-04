/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  com.google.gson.JsonObject
 *  com.google.gson.JsonParser
 *  net.minecraft.client.Minecraft
 *  net.minecraft.network.chat.Component
 *  net.minecraft.network.protocol.common.custom.CustomPacketPayload
 *  net.neoforged.neoforge.common.NeoForge
 *  net.neoforged.neoforge.network.PacketDistributor
 */
package com.primordial.bridge;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.primordial.bridge.AuthCore;
import com.primordial.bridge.Bridge;
import com.primordial.bridge.ClientSkins;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.util.UUID;
import net.minecraft.client.Minecraft;
import net.minecraft.network.chat.Component;
import net.minecraft.network.protocol.common.custom.CustomPacketPayload;
import net.neoforged.neoforge.common.NeoForge;
import net.neoforged.neoforge.network.PacketDistributor;

public final class ClientBridge {
    private static boolean trusted(String string) {
        String string2 = string.split(":")[0];
        return string2.equalsIgnoreCase("mc.primordial.my") || string2.equals("127.0.0.1") || string2.equalsIgnoreCase("localhost");
    }

    public static void register() {
        NeoForge.EVENT_BUS.addListener((net.neoforged.neoforge.client.event.ClientPlayerNetworkEvent.LoggingOut loggingOut) -> ClientSkins.clear());
    }

    public static void receive(Bridge.Message message) {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null) {
            return;
        }
        if (message.action().equals("skin")) {
            ClientSkins.set(message.id(), message.value());
            return;
        }
        if (message.action().equals("error") || message.action().equals("ok")) {
            minecraft.player.displayClientMessage((Component)Component.literal((String)message.value()), false);
            return;
        }
        if (!message.action().equals("challenge")) {
            return;
        }
        if (minecraft.getCurrentServer() == null || !ClientBridge.trusted(minecraft.getCurrentServer().ip)) {
            return;
        }
        Path path = minecraft.gameDirectory.toPath().resolve("primordial-session.json");
        try {
            if (!Files.exists(path, new LinkOption[0])) {
                minecraft.player.displayClientMessage((Component)Component.literal((String)"Start from Primordial Launcher for automatic login."), false);
                return;
            }
            JsonObject jsonObject = JsonParser.parseString((String)Files.readString(path)).getAsJsonObject();
            Files.delete(path);
            if (jsonObject.get("expires").getAsLong() < System.currentTimeMillis()) {
                throw new IllegalStateException("Expired session");
            }
            String string = AuthCore.proof(jsonObject.get("secret").getAsString(), message.value(), UUID.fromString(message.id()));
            PacketDistributor.sendToServer((CustomPacketPayload)new Bridge.Message("proof", jsonObject.get("id").getAsString(), string), (CustomPacketPayload[])new CustomPacketPayload[0]);
        }
        catch (Exception exception) {
            minecraft.player.displayClientMessage((Component)Component.literal((String)"Automatic login needs a fresh session. Relaunch from Primordial Launcher."), false);
        }
    }
}
