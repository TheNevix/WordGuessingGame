<script>
  import { onMount } from "svelte";
  import { page, isWaiting, gameStarted, matchData, matchFoundToast, opponentLeft } from './stores.js';
  import { isTokenExpired } from './stores.js';
  import { tryRefreshToken } from './api.js';
  import { initHub } from './hub.js';
  import { t } from './i18n.js';

  import Login        from './pages/Login.svelte';
  import Register     from './pages/Register.svelte';
  import Dashboard    from './pages/Dashboard.svelte';
  import Challenges   from './pages/Challenges.svelte';
  import Leaderboard  from './pages/Leaderboard.svelte';
  import Lobby        from './pages/Lobby.svelte';
  import PrivateLobby from './pages/PrivateLobby.svelte';
  import JoinPrivate  from './pages/JoinPrivate.svelte';
  import Countdown    from './pages/Countdown.svelte';
  import Waiting      from './pages/Waiting.svelte';
  import Game         from './pages/Game.svelte';
  import Profile      from './pages/Profile.svelte';
  import Ranked       from './pages/Ranked.svelte';
  import BottomNav    from './components/BottomNav.svelte';

  const version = "v1.5.0";

  const TAB_PAGES = ['dashboard', 'challenges', 'leaderboard', 'profile'];

  $: showBottomNav = TAB_PAGES.includes($page) && !$isWaiting && !$gameStarted;

  $: if ($opponentLeft) setTimeout(() => opponentLeft.set(false), 4000);

  onMount(async () => {
    const t = localStorage.getItem("token");
    if ((!t || isTokenExpired(t)) && localStorage.getItem("refreshToken")) {
      await tryRefreshToken();
    }
    initHub();
  });
</script>

{#if $page === 'login' && !$isWaiting && !$gameStarted}
  <Login />
{:else if $page === 'register'}
  <Register />
{:else if $page === 'profile' && !$isWaiting && !$gameStarted}
  <Profile />
{:else if $page === 'challenges' && !$isWaiting && !$gameStarted}
  <Challenges />
{:else if $page === 'leaderboard' && !$isWaiting && !$gameStarted}
  <Leaderboard />
{:else if $page === 'dashboard' && !$isWaiting && !$gameStarted}
  <Dashboard />
{:else if $page === 'lobby' && !$gameStarted}
  <Lobby />
{:else if $page === 'private' && !$isWaiting && !$gameStarted}
  <PrivateLobby />
{:else if $page === 'join-private' && !$isWaiting && !$gameStarted}
  <JoinPrivate />
{:else if $page === 'ranked' && !$gameStarted}
  <Ranked />
{:else if $page === 'countdown' && $matchData}
  <Countdown />
{:else if $isWaiting && !$gameStarted}
  <Waiting />
{:else if $gameStarted && $page === 'game'}
  <Game />
{/if}

{#if showBottomNav}
  <BottomNav />
{/if}

<div class="version-label">{version}</div>

{#if $opponentLeft}
  <div class="toast toast-warning">{$t('game.opponent_left')}</div>
{/if}

{#if $matchFoundToast}
  <div class="match-found-toast">⚔️ {$t('ranked.opponent_found')}</div>
{/if}
