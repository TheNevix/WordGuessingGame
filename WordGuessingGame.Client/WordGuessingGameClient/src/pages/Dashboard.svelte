<script>
  import { onMount } from 'svelte';
  import { page, username, profilePicUrl } from '../stores.js';
  import { handleLogout, fetchStats } from '../api.js';
  import { t } from '../i18n.js';

  let dropdownOpen = false;
  let stats = { gamesPlayed: 0, wins: 0, winRate: 0, streak: 0 };

  onMount(async () => {
    try {
      const data = await fetchStats();
      stats = data;
    } catch { /* keep defaults */ }
  });

  function toggleDropdown() { dropdownOpen = !dropdownOpen; }
  function closeDropdown()  { dropdownOpen = false; }

  function clickOutside(node) {
    const handle = (e) => { if (!node.contains(e.target)) dropdownOpen = false; };
    document.addEventListener('click', handle, true);
    return { destroy() { document.removeEventListener('click', handle, true); } };
  }
</script>

<div class="dashboard-layout">

  <!-- Navbar -->
  <nav class="navbar">
    <span class="navbar-brand">{$t('nav.brand')}</span>
    <div class="navbar-right">
      <div class="nav-dropdown" use:clickOutside>
        <button class="user-badge" on:click={toggleDropdown}>
          <div class="avatar">
            {#if $profilePicUrl}
              <img class="avatar-img" src={$profilePicUrl} alt={$username} />
            {:else}
              {$username.charAt(0).toUpperCase()}
            {/if}
          </div>
          <span class="user-name">{$username}</span>
          <span class="dropdown-caret">{dropdownOpen ? '▴' : '▾'}</span>
        </button>

        {#if dropdownOpen}
          <div class="dropdown-menu">
            <button class="dropdown-item" on:click={() => { page.set('profile'); closeDropdown(); }}>
              {$t('nav.profile')}
            </button>
            <div class="dropdown-divider"></div>
            <button class="dropdown-item dropdown-item-danger" on:click={handleLogout}>
              {$t('nav.logout')}
            </button>
          </div>
        {/if}
      </div>
    </div>
  </nav>

  <!-- Hero banner -->
  <div class="hero-banner">
    <div class="hero-avatar-wrap">
      {#if $profilePicUrl}
        <img class="hero-avatar-img" src={$profilePicUrl} alt={$username} />
      {:else}
        <div class="hero-avatar-ring">{$username.charAt(0).toUpperCase()}</div>
      {/if}
      <div class="online-dot"></div>
    </div>
    <div class="hero-text">
      <h1 class="hero-title">{$t('dashboard.hello', { name: $username })}</h1>
      <p class="hero-subtitle">{$t('dashboard.subtitle')}</p>
      <span class="rank-badge">{$t('dashboard.rank')}</span>
    </div>
    <div class="hero-stats">
      <div class="hero-stat">
        <span class="hero-stat-icon">🎮</span>
        <span class="hero-stat-value">{stats.gamesPlayed}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_games')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">🏆</span>
        <span class="hero-stat-value">{stats.wins}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_wins')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">📊</span>
        <span class="hero-stat-value">{stats.winRate}%</span>
        <span class="hero-stat-label">{$t('dashboard.stat_winrate')}</span>
      </div>
      <div class="hero-stat">
        <span class="hero-stat-icon">🔥</span>
        <span class="hero-stat-value">{stats.streak}</span>
        <span class="hero-stat-label">{$t('dashboard.stat_streak')}</span>
      </div>
    </div>
  </div>

  <!-- Main content -->
  <main class="dashboard-main">

    <p class="section-title">{$t('dashboard.section_play')}</p>
    <div class="play-grid">

      <div class="play-card play-card-primary" role="button" tabindex="0"
        on:click={() => page.set('lobby')}
        on:keydown={(e) => e.key === 'Enter' && page.set('lobby')}>
        <div class="play-card-watermark">⚡</div>
        <div class="play-card-icon">⚡</div>
        <p class="play-card-title">{$t('dashboard.quick_title')}</p>
        <p class="play-card-desc">{$t('dashboard.quick_desc')}</p>
        <button class="play-card-cta">{$t('dashboard.quick_btn')}</button>
      </div>

      <div class="play-card play-card-secondary" role="button" tabindex="0"
        on:click={() => page.set('private')}
        on:keydown={(e) => e.key === 'Enter' && page.set('private')}>
        <div class="play-card-watermark">🔒</div>
        <div class="play-card-icon">🔒</div>
        <p class="play-card-title">{$t('dashboard.private_title')}</p>
        <p class="play-card-desc">{$t('dashboard.private_desc')}</p>
        <button class="play-card-cta">{$t('dashboard.private_btn')}</button>
      </div>

    </div>

    <p class="section-title">{$t('dashboard.section_soon')}</p>
    <div class="feature-grid">

      <div class="feature-card dimmed">
        <span class="badge-soon">{$t('dashboard.soon_badge')}</span>
        <div class="card-icon">🏆</div>
        <p class="card-title">{$t('dashboard.leaderboard_title')}</p>
        <p class="card-desc">{$t('dashboard.leaderboard_desc')}</p>
      </div>

      <div class="feature-card dimmed">
        <span class="badge-soon">{$t('dashboard.soon_badge')}</span>
        <div class="card-icon">📜</div>
        <p class="card-title">{$t('dashboard.history_title')}</p>
        <p class="card-desc">{$t('dashboard.history_desc')}</p>
      </div>

      <div class="feature-card dimmed">
        <span class="badge-soon">{$t('dashboard.soon_badge')}</span>
        <div class="card-icon">👥</div>
        <p class="card-title">{$t('dashboard.friends_title')}</p>
        <p class="card-desc">{$t('dashboard.friends_desc')}</p>
      </div>

    </div>
  </main>

</div>
