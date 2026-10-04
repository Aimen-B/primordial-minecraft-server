import com.primordial.bridge.AuthCore;
import java.util.UUID;
public class AuthCoreTest {
 public static void main(String[] args) throws Exception {
  AuthCore core=new AuthCore();UUID player=UUID.randomUUID();
  var ticket=core.issue("Friend","ember_mage");String challenge=core.nonce();
  String proof=AuthCore.proof(ticket.secret(),challenge,player);
  if(core.consume(ticket.id(),"Other",challenge,player,proof))throw new AssertionError("Identity mismatch accepted");
  if(core.consume(ticket.id(),"Friend",core.nonce(),player,proof))throw new AssertionError("Challenge mismatch accepted");
  if(core.consume(ticket.id(),"Friend",challenge,UUID.randomUUID(),proof))throw new AssertionError("UUID mismatch accepted");
  if(!core.consume(ticket.id(),"Friend",challenge,player,proof))throw new AssertionError("Valid proof rejected");
  if(core.consume(ticket.id(),"Friend",challenge,player,proof))throw new AssertionError("Replay accepted");
  var old=core.issue("Friend");core.issue("Friend");
  if(core.consume(old.id(),"Friend",challenge,player,AuthCore.proof(old.secret(),challenge,player)))throw new AssertionError("Old ticket accepted");
  for(int i=0;i<6;i++)if(!core.allow("name:test",6))throw new AssertionError("Premature rate limit");
  if(core.allow("name:test",6))throw new AssertionError("Rate limit missing");
  System.out.println("PASS: identity, challenge, UUID binding; replay and old-ticket rejection; rate limiting.");
 }
}
