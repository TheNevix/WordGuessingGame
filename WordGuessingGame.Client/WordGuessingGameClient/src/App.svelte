<script>
  import { onMount } from "svelte";
  import { page, isWaiting, gameStarted, matchData } from './stores.js';
  import { isTokenExpired } from './stores.js';
  import { tryRefreshToken } from './api.js';
  import { initHub } from './hub.js';

  import Login       from './pages/Login.svelte';
  import Register    from './pages/Register.svelte';
  import Dashboard   from './pages/Dashboard.svelte';
  import Lobby       from './pages/Lobby.svelte';
  import PrivateLobby from './pages/PrivateLobby.svelte';
  import JoinPrivate from './pages/JoinPrivate.svelte';
  import Countdown   from './pages/Countdown.svelte';
  import Waiting     from './pages/Waiting.svelte';
  import Game        from './pages/Game.svelte';
  import Profile     from './pages/Profile.svelte';

  const version = "v1.4.0";

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
{:else if $page === 'dashboard' && !$isWaiting && !$gameStarted}
  <Dashboard />
{:else if $page === 'lobby' && !$gameStarted}
  <Lobby />
{:else if $page === 'private' && !$isWaiting && !$gameStarted}
  <PrivateLobby />
{:else if $page === 'join-private' && !$isWaiting && !$gameStarted}
  <JoinPrivate />
{:else if $page === 'countdown' && $matchData}
  <Countdown />
{:else if $isWaiting && !$gameStarted}
  <Waiting />
{:else if $gameStarted && $page === 'game'}
  <Game />
{/if}

<div class="version-label">{version}</div>
