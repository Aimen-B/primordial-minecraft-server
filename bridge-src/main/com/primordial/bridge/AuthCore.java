/*
 * Decompiled with CFR 0.152.
 */
package com.primordial.bridge;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Base64;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;

public final class AuthCore {
    private final Map<String, Ticket> tickets = new ConcurrentHashMap<String, Ticket>();
    private final Map<String, Window> limits = new ConcurrentHashMap<String, Window>();
    private final SecureRandom random = new SecureRandom();

    public synchronized boolean allow(String string, int n) {
        long l = System.currentTimeMillis();
        this.limits.entrySet().removeIf(entry -> l - ((Window)entry.getValue()).start > 60000L);
        Window window = this.limits.get(string);
        if (window == null) {
            this.limits.put(string, new Window(l, 1));
            return true;
        }
        if (window.count >= n) {
            return false;
        }
        this.limits.put(string, new Window(window.start, window.count + 1));
        return true;
    }

    public synchronized Ticket issue(String string) {
        return this.issue(string, "default");
    }

    public synchronized Ticket issue(String string, String string2) {
        long l = System.currentTimeMillis();
        this.tickets.values().removeIf(ticket -> ticket.expires < l || ticket.name.equals(string));
        if (this.tickets.size() > 1000) {
            throw new IllegalStateException("Too many sessions.");
        }
        Ticket ticket2 = new Ticket(UUID.randomUUID().toString(), string, this.nonce(), l + 120000L, string2);
        this.tickets.put(ticket2.id, ticket2);
        return ticket2;
    }

    public synchronized String skin(String string) {
        Ticket ticket = this.tickets.get(string);
        return ticket == null ? "default" : ticket.skin;
    }

    public String nonce() {
        byte[] byArray = new byte[32];
        this.random.nextBytes(byArray);
        return Base64.getEncoder().encodeToString(byArray);
    }

    public static String proof(String string, String string2, UUID uUID) throws Exception {
        Mac mac = Mac.getInstance("HmacSHA256");
        mac.init(new SecretKeySpec(Base64.getDecoder().decode(string), "HmacSHA256"));
        return Base64.getEncoder().encodeToString(mac.doFinal((string2 + ":" + String.valueOf(uUID)).getBytes(StandardCharsets.UTF_8)));
    }

    public synchronized boolean consume(String string, String string2, String string3, UUID uUID, String string4) {
        Ticket ticket = this.tickets.get(string);
        if (ticket == null || ticket.expires < System.currentTimeMillis() || !ticket.name.equals(string2)) {
            return false;
        }
        try {
            boolean bl = MessageDigest.isEqual(Base64.getDecoder().decode(AuthCore.proof(ticket.secret, string3, uUID)), Base64.getDecoder().decode(string4));
            if (bl) {
                this.tickets.remove(string);
            }
            return bl;
        }
        catch (Exception exception) {
            return false;
        }
    }

    private record Window(long start, int count) {
    }

    public record Ticket(String id, String name, String secret, long expires, String skin) {
    }
}

