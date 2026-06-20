<script>
  import { onMount } from 'svelte';
  import { get } from 'svelte/store';
  import { goto } from '$app/navigation';
  import { isReconnecting } from '$lib/stores.js';

  onMount(async () => {
    const res = await fetch('/api/auth/token');
    const { token } = await res.json();
    // Skip redirect if a reconnect is already in progress
    if (!token && !get(isReconnecting)) goto('/inloggen');
  });
</script>

<slot />
