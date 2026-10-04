/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  net.minecraft.server.MinecraftServer
 *  net.minecraft.server.level.ServerPlayer
 */
package com.codex.forgelogin;

import com.codex.forgelogin.ForgeLoginMod;
import java.io.Closeable;
import java.io.IOException;
import java.io.OutputStream;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.nio.channels.FileChannel;
import java.nio.channels.spi.AbstractInterruptibleChannel;
import java.nio.file.Files;
import java.nio.file.OpenOption;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.nio.file.StandardOpenOption;
import java.nio.file.attribute.FileAttribute;
import java.util.Arrays;
import java.util.Locale;
import java.util.Properties;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;

public final class AuthHooks {
    private static Object instance() throws Exception {
        return ForgeLoginMod.class.getField("BRIDGE_INSTANCE").get(null);
    }

    private static Object store() throws Exception {
        Field field = ForgeLoginMod.class.getDeclaredField("passwordStore");
        field.setAccessible(true);
        return field.get(AuthHooks.instance());
    }

    private static Object call(Object object, String string, Class<?>[] classArray, Object ... objectArray) throws Exception {
        Method method = object.getClass().getDeclaredMethod(string, classArray);
        method.setAccessible(true);
        return method.invoke(object, objectArray);
    }

    public static boolean whitelisted(MinecraftServer minecraftServer, String string) {
        return minecraftServer.getPlayerList().isUsingWhitelist() && Arrays.asList(minecraftServer.getPlayerList().getWhiteListNames()).contains(string);
    }

    public static boolean registered(String string) throws Exception {
        return (Boolean)AuthHooks.call(AuthHooks.store(), "hasUser", new Class[]{String.class}, string.toLowerCase(Locale.ROOT));
    }

    public static String authenticate(MinecraftServer minecraftServer, String string, String string2, boolean bl) throws Exception {
        boolean bl2;
        if (!minecraftServer.isSameThread()) {
            throw new IllegalStateException("Authentication must run on the server thread.");
        }
        if (!AuthHooks.whitelisted(minecraftServer, string)) {
            return "not_whitelisted";
        }
        if (string2.length() == 0 || string2.length() > 128 || bl && string2.length() < 8) {
            return "invalid_password";
        }
        Object object = AuthHooks.store();
        String string3 = string.toLowerCase(Locale.ROOT);
        boolean bl3 = (Boolean)AuthHooks.call(object, "hasUser", new Class[]{String.class}, string3);
        if (bl && bl3) {
            return "already_claimed";
        }
        if (!bl && !bl3) {
            return "unclaimed";
        }
        Field usersField=object.getClass().getDeclaredField("users");usersField.setAccessible(true);
        Properties users=(Properties)usersField.get(object);String previous=users.getProperty(string3);
        boolean bl4 = bl2 = bl ? ((Boolean)AuthHooks.call(object, "setPassword", new Class[]{String.class, String.class}, string3, string2)).booleanValue() : ((Boolean)AuthHooks.call(object, "verifyPassword", new Class[]{String.class, String.class}, string3, string2)).booleanValue();
        if(bl && !bl2) { if(previous==null)users.remove(string3);else users.setProperty(string3,previous); }
        return bl2 ? "ok" : (bl ? "storage_error" : "wrong_password");
    }

    public static boolean loggedIn(ServerPlayer serverPlayer) throws Exception {
        return (Boolean)AuthHooks.call(AuthHooks.instance(), "isLoggedIn", new Class[]{ServerPlayer.class}, serverPlayer);
    }

    public static void unlock(ServerPlayer serverPlayer) throws Exception {
        if (!AuthHooks.whitelisted(serverPlayer.server, serverPlayer.getGameProfile().getName())) {
            throw new IllegalStateException("Nickname is not approved.");
        }
        AuthHooks.call(AuthHooks.instance(), "unlockPlayer", new Class[]{ServerPlayer.class, String.class}, serverPlayer, "Logged in automatically. Have fun!");
    }

    /*
     * WARNING - Removed try catching itself - possible behaviour change.
     * Enabled aggressive block sorting
     * Enabled unnecessary exception pruning
     * Enabled aggressive exception aggregation
     */
    public static boolean atomicSave(Properties users,Path path) {
        Path temp=null;
        try {
            Files.createDirectories(path.getParent());
            temp=Files.createTempFile(path.getParent(),"users-",".tmp");
            try(OutputStream output=Files.newOutputStream(temp)) {users.store(output,"ForgeLoginMod users. Password hashes.");}
            try(FileChannel channel=FileChannel.open(temp,StandardOpenOption.WRITE)){channel.force(true);}
            Files.move(temp,path,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);return true;
        }catch(IOException error){return false;}
        finally {if(temp!=null)try{Files.deleteIfExists(temp);}catch(IOException ignored){}}
    }
}
