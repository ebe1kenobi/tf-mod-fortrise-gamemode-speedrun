using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseSpeedRun
{
  // Logique de round du mode Loop Scroll.
  //
  // Etape actuelle (fondation) : copie conforme de LastManStandingRoundLogic
  // (spawn FFA, coffres, fin de round au dernier survivant). Le scrolling caméra,
  // le mur invisible et le wrap relatif-caméra seront ajoutes dans OnUpdate lors
  // des etapes suivantes.
  public class SpeedRunRoundLogic : RoundLogic
  {
    private RoundEndCounter roundEndCounter;
    private bool done;
    private bool wasFinalKill;

    public SpeedRunRoundLogic(Session session) : base(session, true)
    {
      this.roundEndCounter = new RoundEndCounter(session);
      // Pas de miasma anti-stalling : le round dure longtemps (grand niveau qui
      // scrolle), le miasma se declencherait et tuerait tout le monde.
      this.CanMiasma = false;
    }

    public override void OnLevelLoadFinish()
    {
      base.OnLevelLoadFinish();
      base.Session.CurrentLevel.Add<VersusStart>(new VersusStart(base.Session));
      base.Players = base.SpawnPlayersFFA();
      // Reset du parcours et de l'intro AVANT le debut du round : le zoom d'intro
      // demarre pendant la sequence VersusStart, donc camX/camY doivent deja etre
      // repositionnes au depart pour ce round.
      this.scrollInit = false;
      this.zoomStarted = false;
      this.zoomInT = 0f;
      this.preRoundTimer = 0f;
    }

    public override void OnRoundStart()
    {
      base.OnRoundStart();
      base.SpawnTreasureChestsVersus();
      this.scrollInit = false;
      this.offscreenTimer = new float[TFGame.Players.Length];
      this.playerDetached = new bool[TFGame.Players.Length];
    }

    public override void OnUpdate()
    {
      SessionStats.TimePlayed += Engine.DeltaTicks;
      base.OnUpdate();

      UpdateScroll();

      if (base.RoundStarted && !this.done && base.Session.CurrentLevel.Ending && base.Session.CurrentLevel.CanEnd)
      {
        if (!this.roundEndCounter.Finished)
        {
          this.roundEndCounter.Update();
          return;
        }
        this.done = true;
        if (base.Session.CurrentLevel.Players.Count == 1)
        {
          base.AddScore(base.Session.CurrentLevel.Player.PlayerIndex, 1);
        }
        base.InsertCrownEvent();
        base.Session.EndRound();
      }
    }

    // Largeur de la fenetre visible (320 normal, plus si option "wide screen").
    private static float WinW
    {
      get
      {
        Monocle.Screen s = Engine.Instance != null ? Engine.Instance.Screen : null;
        return s != null ? (float)s.Width : 320f;
      }
    }

    // Marge (px) pour garder le joueur entierement dans la fenetre visible.
    private const float WINDOW_MARGIN = 6f;
    // Coins arrondis ELLIPTIQUES : rayon horizontal (commence a tourner tot) et
    // rayon vertical (le tour couvre toute la hauteur). Bornes a la moitie du cote.
    private const float TURN_RADIUS_X = 220f;
    // Suivi du leader : ou le garder dans la fenetre (fraction), gain, et vitesse
    // max (px/frame) pour eviter un plongeon brutal sur une chute.
    private const float LEADER_TARGET = 0.667f;
    private const float LEADER_GAIN = 0.08f;
    private const float LEADER_MAX_SPEED = 6f;

    private bool scrollInit;
    private bool loopMode;
    private float camX, camY;
    private float bandMaxCamX;
    private float[] offscreenTimer; // par PlayerIndex, en frames accumulees
    private bool[] playerDetached;  // par PlayerIndex : sorti de l'ecran, non clampe

    // Parcours pre-echantillonne en polyligne fine (points ~1px) + longueurs
    // cumulees, pour un deplacement a VITESSE CONSTANTE (fluide) le long des courbes.
    private Vector2[] pts;
    private float[] cum;
    private float pathLen;
    private float pathS;

    private void InitScroll()
    {
      camX = 0f;
      camY = 0f;
      pathS = 0f;
      loopMode = SpeedRunLevelSystem.IsLoop;
      bandMaxCamX = Math.Max(0f, SpeedRunLevelSystem.TotalWidthPixels - WinW);
      if (loopMode)
        BuildLoopPath(Math.Max(0f, SpeedRunLevelSystem.TotalWidthPixels - WinW),
                      Math.Max(0f, SpeedRunLevelSystem.TotalHeightPixels - 240f));
      scrollInit = true;
    }

    // Rectangle a coins arrondis elliptiques. Le coin haut-gauche reste net (depart
    // propre sur le bloc de spawn) ; les 3 autres sont arrondis.
    private void BuildLoopPath(float maxX, float maxY)
    {
      float rx = Math.Min(TURN_RADIUS_X, maxX / 2f);
      float ry = maxY / 2f; // le tour vertical couvre toute la hauteur
      if (rx < 1f) rx = 0f;
      if (ry < 1f) ry = 0f;

      Vector2 c0 = new Vector2(0f, 0f);
      Vector2 c1 = new Vector2(maxX, 0f);
      Vector2 c2 = new Vector2(maxX, maxY);
      Vector2 c3 = new Vector2(0f, maxY);

      List<Vector2> fine = new List<Vector2>();
      EmitStraight(fine, c0, new Vector2(maxX - rx, 0f));
      EmitCurve(fine, new Vector2(maxX - rx, 0f), c1, new Vector2(maxX, ry));
      EmitStraight(fine, new Vector2(maxX, ry), new Vector2(maxX, maxY - ry));
      EmitCurve(fine, new Vector2(maxX, maxY - ry), c2, new Vector2(maxX - rx, maxY));
      EmitStraight(fine, new Vector2(maxX - rx, maxY), new Vector2(rx, maxY));
      EmitCurve(fine, new Vector2(rx, maxY), c3, new Vector2(0f, maxY - ry));
      EmitStraight(fine, new Vector2(0f, maxY - ry), c0);

      pts = fine.ToArray();
      cum = new float[pts.Length];
      cum[0] = 0f;
      for (int i = 1; i < pts.Length; i++)
        cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).Length();
      pathLen = cum[pts.Length - 1];
    }

    // Ajoute les points d'un segment droit (~1px d'espacement), sans dupliquer le
    // point de depart (deja present a la fin du segment precedent).
    private static void EmitStraight(List<Vector2> fine, Vector2 a, Vector2 b)
    {
      if (fine.Count == 0)
        fine.Add(a);
      int steps = Math.Max(1, (int)Math.Ceiling((b - a).Length()));
      for (int i = 1; i <= steps; i++)
        fine.Add(Vector2.Lerp(a, b, i / (float)steps));
    }

    private static void EmitCurve(List<Vector2> fine, Vector2 a, Vector2 c, Vector2 b)
    {
      if (fine.Count == 0)
        fine.Add(a);
      int steps = Math.Max(2, (int)Math.Ceiling(BezierLen(a, c, b)));
      for (int i = 1; i <= steps; i++)
        fine.Add(Bez(a, c, b, i / (float)steps));
    }

    private static Vector2 Bez(Vector2 a, Vector2 c, Vector2 b, float t)
    {
      float u = 1f - t;
      return (u * u) * a + (2f * u * t) * c + (t * t) * b;
    }

    private static float BezierLen(Vector2 a, Vector2 c, Vector2 b)
    {
      Vector2 prev = a;
      float len = 0f;
      const int n = 16;
      for (int i = 1; i <= n; i++)
      {
        Vector2 p = Bez(a, c, b, i / (float)n);
        len += (p - prev).Length();
        prev = p;
      }
      return len;
    }

    private Vector2 PathPos(float s)
    {
      if (pts == null || pts.Length < 2 || pathLen <= 0f)
        return new Vector2(camX, camY);
      s %= pathLen;
      if (s < 0f)
        s += pathLen;

      // recherche binaire du plus grand i avec cum[i] <= s
      int lo = 0, hi = pts.Length - 1;
      while (lo < hi)
      {
        int mid = (lo + hi + 1) / 2;
        if (cum[mid] <= s) lo = mid;
        else hi = mid - 1;
      }
      int i = lo;
      int i2 = (i + 1 < pts.Length) ? i + 1 : i;
      float segLen = cum[i2] - cum[i];
      float f = segLen > 0f ? (s - cum[i]) / segLen : 0f;
      return Vector2.Lerp(pts[i], pts[i2], f);
    }

    // Intro : vue d'ensemble dezoomee du niveau entier, puis zoom (ease) vers la
    // fenetre de depart. Le zoom demarre PENDANT la sequence d'intro VersusStart
    // (avant le FIGHT) : au round 1 c'est l'appui qui ferme la carte de la tour
    // (MenuInput.Confirm) qui le declenche ; aux rounds suivants (pas d'appui),
    // il demarre apres un court maintien de la vue d'ensemble.
    private const float ZOOM_IN_FRAMES = 90f;        // ~1,5 s a 60 fps
    private const float OVERVIEW_HOLD_FRAMES = 20f;  // maintien avant zoom (rounds 2+)
    private float zoomInT;
    private bool zoomStarted;
    private float preRoundTimer;

    private void SetCamera(Level level, Vector2 center, float zoom)
    {
      level.Camera.Zoom = zoom;
      level.Camera.Position = center - new Vector2(WinW / 2f, 120f) / zoom;
      KillVanillaBorder(level);
    }

    // Le jeu dessine une barre noire de 2px (Level.CoreRender) quand Camera.X/Y != 0,
    // gardee par `Camera.Origin == Vector2.Zero`. En wide-screen elle tombe en plein
    // milieu. On met une origine minuscule non nulle : le test echoue (barre non
    // dessinee) mais la vue ne bouge pas (l'origine est castee en int dans la matrice).
    private static void KillVanillaBorder(Level level)
    {
      level.Camera.Origin = new Vector2(0.0001f, 0f);
    }

    private void UpdateScroll()
    {
      Level level = base.Session.CurrentLevel;
      if (level == null)
        return;

      if (!this.scrollInit)
        InitScroll();

      // Zoom pour faire tenir tout le niveau dans la fenetre visible.
      float fitZoom = Math.Min(WinW / SpeedRunLevelSystem.TotalWidthPixels,
                               240f / SpeedRunLevelSystem.TotalHeightPixels);
      Vector2 levelCenter = new Vector2(SpeedRunLevelSystem.TotalWidthPixels / 2f,
                                        SpeedRunLevelSystem.TotalHeightPixels / 2f);

      // Option desactivee : pas de vue d'ensemble ni d'animation, la camera se
      // place directement sur la fenetre de depart.
      if (!TFModFortRiseSpeedRunModule.Settings.SpeedRunIntroZoom)
      {
        zoomStarted = true;
        zoomInT = 1f;
      }

      if (!base.RoundStarted && !zoomStarted)
      {
        // Vue d'ensemble tant que le zoom n'est pas declenche.
        preRoundTimer += Engine.TimeMult;
        zoomInT = 0f;
        SetCamera(level, levelCenter, fitZoom);
        bool trigger = base.Session.RoundIndex == 0
          ? MenuInput.Confirm
          : preRoundTimer >= OVERVIEW_HOLD_FRAMES;
        if (trigger)
          zoomStarted = true;
        return;
      }

      if (zoomInT < 1f)
      {
        // Zoom anime de la vue d'ensemble vers la fenetre de depart (pendant
        // l'intro, avant le FIGHT).
        zoomInT = Math.Min(1f, zoomInT + Engine.TimeMult / ZOOM_IN_FRAMES);
        float t = zoomInT * zoomInT * (3f - 2f * zoomInT); // smoothstep
        Vector2 startCenter = new Vector2(camX + WinW / 2f, camY + 120f);
        SetCamera(level, Vector2.Lerp(levelCenter, startCenter, t), MathHelper.Lerp(fitZoom, 1f, t));
        if (zoomInT < 1f)
          return; // pas de scroll ni de murs pendant l'animation
      }

      if (!base.RoundStarted)
      {
        // Zoom termine avant le debut du round : tenir la fenetre de depart.
        SetCamera(level, new Vector2(camX + WinW / 2f, camY + 120f), 1f);
        return;
      }

      level.Camera.Zoom = 1f;

      float baseSpeed = TFModFortRiseSpeedRunModule.Settings.SpeedRunSpeed / 10f * Engine.TimeMult;
      bool followLeader = TFModFortRiseSpeedRunModule.Settings.SpeedRunFollowLeader;
      bool leaveBehind = TFModFortRiseSpeedRunModule.Settings.SpeedRunLeaveBehind;

      int dirX, dirY;
      if (loopMode)
      {
        // Direction courante (tangente du parcours) AVANT d'avancer.
        Vector2 curPos = PathPos(pathS);
        Vector2 tangent = PathPos(pathS + 2f) - curPos;
        dirX = Math.Abs(tangent.X) > 0.35f ? Math.Sign(tangent.X) : 0;
        dirY = Math.Abs(tangent.Y) > 0.35f ? Math.Sign(tangent.Y) : 0;
        camX = curPos.X;
        camY = curPos.Y;

        float speed = followLeader ? LeaderControlledSpeed(level, baseSpeed, dirX, dirY) : baseSpeed;
        pathS += speed;
        if (pathS >= pathLen)
          pathS -= pathLen;
        Vector2 pos = PathPos(pathS);
        camX = pos.X;
        camY = pos.Y;
      }
      else
      {
        dirX = 1;
        dirY = 0;
        float speed = followLeader ? LeaderControlledSpeed(level, baseSpeed, dirX, dirY) : baseSpeed;
        camX = Math.Min(camX + speed, bandMaxCamX);
      }

      // Etat partage lu par les patches de wrap et le clamp.
      SpeedRunState.CamX = camX;
      SpeedRunState.CamY = camY;
      SpeedRunState.DirX = dirX;
      SpeedRunState.DirY = dirY;

      level.Camera.X = camX;
      level.Camera.Y = camY;
      KillVanillaBorder(level);

      ClampPlayersToWindow(level, dirX, dirY, leaveBehind);
      if (leaveBehind)
        OffscreenDeathCheck(level);
    }

    // Vitesse de scroll pilotee par le joueur le plus avance (le "leader"), qu'on
    // vise a garder vers LEADER_TARGET de la fenetre dans la direction du scroll.
    // Plancher = vitesse de base (jamais en arriere) ; plafond = LEADER_MAX_SPEED
    // pour ne pas plonger brutalement quand quelqu'un tombe.
    private float LeaderControlledSpeed(Level level, float baseSpeed, int dirX, int dirY)
    {
      bool horiz = dirX != 0 && dirY == 0;
      bool vert = dirY != 0 && dirX == 0;
      if (!horiz && !vert)
        return baseSpeed; // virage diagonal : vitesse de base

      Player leader = null;
      float best = float.NegativeInfinity;
      foreach (Entity e in level.Players)
      {
        Player p = e as Player;
        if (p == null || p.Dead)
          continue;
        float proj = horiz ? p.X * dirX : p.Y * dirY;
        if (proj > best)
        {
          best = proj;
          leader = p;
        }
      }
      if (leader == null)
        return baseSpeed;

      float winW = WinW;
      float frac = horiz
        ? (dirX > 0 ? (leader.X - camX) : (camX + winW - leader.X)) / winW
        : (dirY > 0 ? (leader.Y - camY) : (camY + 240f - leader.Y)) / 240f;

      float w = horiz ? winW : 240f;
      float speed = baseSpeed + LEADER_GAIN * (frac - LEADER_TARGET) * w;

      float cap = LEADER_MAX_SPEED * Engine.TimeMult;
      if (speed < baseSpeed) speed = baseSpeed;
      if (speed > cap) speed = cap;
      return speed;
    }

    private void OffscreenDeathCheck(Level level)
    {
      if (offscreenTimer == null)
        return;
      float threshold = TFModFortRiseSpeedRunModule.Settings.SpeedRunOffscreenDeathDelay * 60f;

      foreach (Entity e in level.Players)
      {
        Player p = e as Player;
        if (p == null || p.Dead)
          continue;
        int idx = p.PlayerIndex;
        if (idx < 0 || idx >= offscreenTimer.Length)
          continue;

        bool offscreen = p.X < camX || p.X > camX + WinW || p.Y < camY || p.Y > camY + 240f;
        if (offscreen)
        {
          offscreenTimer[idx] += Engine.TimeMult;
          if (false && offscreenTimer[idx] >= threshold)//todo
            p.Die(DeathCause.Miasma, -1);
        }
        else
        {
          offscreenTimer[idx] = 0f;
        }
      }
    }

    // Mur invisible suivant la camera. Bord AVANT (sens du scroll) : en horizontal
    // on le bloque, en vertical on le laisse libre (gravite). Bord ARRIERE : si
    // leaveBehind, on le laisse LIBRE (le joueur sort de l'ecran, mort geree par
    // OffscreenDeathCheck) ; sinon on le bloque et on ecrase (Squish) si coince.
    private void ClampPlayersToWindow(Level level, int dirX, int dirY, bool leaveBehind)
    {
      bool clampX = dirX != 0;
      bool clampY = dirY != 0;
      float winW = WinW;
      float minX = camX + WINDOW_MARGIN;
      float maxX = camX + winW - WINDOW_MARGIN;
      float minY = camY + WINDOW_MARGIN;
      float maxY = camY + 240f - WINDOW_MARGIN;

      // Tolerance au-dela de la fenetre : en-deca on clampe encore (joueur en
      // train de sortir), au-dela le joueur est considere comme largue.
      const float OFFSCREEN_TOLERANCE = 40f;

      foreach (Entity entity in level.Players)
      {
        Player player = entity as Player;
        if (player == null || player.Dead)
          continue;

        // Hysteresis "largue / rattache" : un joueur laisse derriere ne doit PAS
        // etre clampe — surtout pas par le bord AVANT quand la camera revient
        // vers lui apres un tour de boucle (sinon il est harponne au bord et
        // traine dans les murs). Une fois largue, il ne redevient clampable que
        // quand la fenetre le contient a nouveau ENTIEREMENT.
        if (leaveBehind && playerDetached != null)
        {
          int idx = player.PlayerIndex;
          if (idx >= 0 && idx < playerDetached.Length)
          {
            // Rattacher seulement quand le joueur est deja DANS la zone de clamp
            // (marge comprise) : si on rattachait des que le bord avant le touche,
            // le clamp le pousserait de WINDOW_MARGIN vers l'interieur (~6-10px de
            // decalage constate au retour de la camera).
            bool inside = player.X >= minX && player.X <= maxX
                       && player.Y >= minY && player.Y <= maxY;
            if (playerDetached[idx])
            {
              if (!inside)
                continue; // toujours largue : on ne le touche pas
              playerDetached[idx] = false; // la camera l'a recouvre : rattache
            }
            else
            {
              bool farOutside = player.X < camX - OFFSCREEN_TOLERANCE
                             || player.X > camX + winW + OFFSCREEN_TOLERANCE
                             || player.Y < camY - OFFSCREEN_TOLERANCE
                             || player.Y > camY + 240f + OFFSCREEN_TOLERANCE;
              if (farOutside)
              {
                playerDetached[idx] = true;
                continue;
              }
            }
          }
        }

        // Le bord AVANT ne retient qu'un joueur qui se deplace VERS lui (il
        // empeche de courir devant la camera). Sans cette condition de vitesse,
        // il "ramassait" les joueurs immobiles balayes par le bord (ex: joueur
        // laisse sur la tranche entre deux blocs, decale de ~WINDOW_MARGIN a
        // l'activation de l'axe apres un virage).
        if (clampX)
        {
          if (dirX > 0)
          {
            // Scroll droite : bord avant = droite (bloque), arriere = gauche.
            if (player.X > maxX && player.Speed.X > 0f)
              player.X = maxX;
            else if (player.X < minX && !leaveBehind)
            {
              if (player.CollideCheck(GameTags.Solid, new Vector2(minX, player.Y)))
              {
                player.Die(DeathCause.Squish, -1);
                continue;
              }
              player.X = minX;
            }
          }
          else
          {
            // Scroll gauche : bord avant = gauche (bloque), arriere = droite.
            if (player.X < minX && player.Speed.X < 0f)
              player.X = minX;
            else if (player.X > maxX && !leaveBehind)
            {
              if (player.CollideCheck(GameTags.Solid, new Vector2(maxX, player.Y)))
              {
                player.Die(DeathCause.Squish, -1);
                continue;
              }
              player.X = maxX;
            }
          }
        }

        // Axe vertical : bord avant libre (gravite). Bord arriere bloque/ecrase
        // sauf si leaveBehind.
        if (clampY && !leaveBehind)
        {
          if (dirY > 0)
          {
            // Descente : bord arriere = haut (minY).
            if (player.Y < minY)
            {
              if (player.CollideCheck(GameTags.Solid, new Vector2(player.X, minY)))
              {
                player.Die(DeathCause.Squish, -1);
                continue;
              }
              player.Y = minY;
            }
          }
          else if (dirY < 0)
          {
            // Montee : bord arriere = bas (maxY).
            if (player.Y > maxY)
            {
              if (player.CollideCheck(GameTags.Solid, new Vector2(player.X, maxY)))
              {
                player.Die(DeathCause.Squish, -1);
                continue;
              }
              player.Y = maxY;
            }
          }
        }
      }
    }

    public override void OnPlayerDeath(Player player, PlayerCorpse corpse, int playerIndex, DeathCause deathType, Vector2 position, int killerIndex)
    {
      base.OnPlayerDeath(player, corpse, playerIndex, deathType, position, killerIndex);
      if (this.wasFinalKill && base.Session.CurrentLevel.LivingPlayers == 0)
      {
        base.CancelFinalKill();
        return;
      }
      if (base.FFACheckForAllButOneDead())
      {
        int num = -1;
        foreach (Entity entity in base.Session.CurrentLevel[GameTags.Player])
        {
          Player player2 = (Player)entity;
          if (!player2.Dead)
          {
            num = player2.PlayerIndex;
            break;
          }
        }
        base.Session.CurrentLevel.Ending = true;
        if (num != -1 && base.Session.Scores[num] >= base.Session.MatchSettings.GoalScore - 1)
        {
          this.wasFinalKill = true;
          base.FinalKill(corpse, num);
        }
      }
    }
  }
}
