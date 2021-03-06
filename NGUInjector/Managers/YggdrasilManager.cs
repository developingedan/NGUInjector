﻿using System;
using static NGUInjector.Main;

namespace NGUInjector.Managers
{
    internal class YggdrasilManager
    {
        private readonly Character _character;

        public YggdrasilManager()
        {
            _character = Main.Character;
        }

        bool NeedsHarvest()
        {
            return _character.yggdrasilController.anyFruitMaxxed();
        }

        internal void ManageYggHarvest()
        {
            //We need to harvest but we dont have a loadout to manage OR we're not managing loadout
            if (!Settings.SwapYggdrasilLoadouts || Settings.YggdrasilLoadout.Length == 0)
            {
                //Not sure why this would be true, but safety first
                if (LoadoutManager.CurrentLock == LockType.Yggdrasil)
                {
                    LoadoutManager.RestoreGear();
                    LoadoutManager.ReleaseLock();
                }

                if (DiggerManager.CurrentLock == LockType.Yggdrasil)
                {
                    DiggerManager.RestoreDiggers();
                    DiggerManager.ReleaseLock();
                }
                ActuallyHarvest();
                return;
            }

            //We dont need to harvest anymore and we've already swapped, so swap back
            if (!NeedsHarvest() && LoadoutManager.CurrentLock == LockType.Yggdrasil)
            {
                LoadoutManager.RestoreGear();
                LoadoutManager.ReleaseLock();
            }

            if (!NeedsHarvest() && DiggerManager.CurrentLock == LockType.Yggdrasil)
            {
                DiggerManager.RestoreDiggers();
                DiggerManager.ReleaseLock();
            }

            //We're managing loadouts
            if (NeedsHarvest())
            {
                if (!LoadoutManager.TryYggdrasilSwap() || !DiggerManager.TryYggSwap())
                    return;

                Log("Equipping Loadout for Yggdrasil and Harvesting");
                //We swapped so harvest
                ActuallyHarvest();
                LoadoutManager.RestoreGear();
                LoadoutManager.ReleaseLock();
                DiggerManager.RestoreDiggers();
                DiggerManager.ReleaseLock();
            }
        }

        private void ActuallyHarvest()
        {
            var currentPage = _character.yggdrasilController.curPage;
            _character.yggdrasilController.changePage(0);
            _character.yggdrasilController.consumeAll();
            _character.yggdrasilController.changePage(1);
            _character.yggdrasilController.consumeAll();
            _character.yggdrasilController.changePage(2);
            _character.yggdrasilController.consumeAll();
            _character.yggdrasilController.changePage(currentPage);
            _character.yggdrasilController.refreshMenu();
        }

        internal void CheckFruits()
        {
            if (!Settings.ActivateFruits)
                return;
            var curPage = _character.yggdrasilController.curPage;
            for (var i = 0; i < _character.yggdrasil.fruits.Count; i++)
            {
                var fruit = _character.yggdrasil.fruits[i];
                //Skip inactive fruits
                if (fruit.maxTier == 0L)
                    continue;

                //Skip fruits that are permed
                if (fruit.permCostPaid)
                    continue;

                if (fruit.activated)
                    continue;

                if (_character.yggdrasilController.usesEnergy[i] &&
                    _character.curEnergy >= _character.yggdrasilController.activationCost[i])
                {
                    Log($"Removing energy for fruit {i}");
                    _character.removeMostEnergy();
                    var slot = ChangePage(i);
                    _character.yggdrasilController.fruits[slot].activate(i);
                    continue;
                }

                if (!_character.yggdrasilController.usesEnergy[i] &&
                    _character.magic.curMagic >= _character.yggdrasilController.activationCost[i])
                {
                    Log($"Removing magic for fruit {i}");
                    _character.removeMostMagic();
                    var slot = ChangePage(i);
                    _character.yggdrasilController.fruits[slot].activate(i);
                }
            }
            _character.yggdrasilController.changePage(curPage);
        }

        private int ChangePage(int slot)
        {
            var page = (int)Math.Floor((double)slot / 9);
            _character.yggdrasilController.changePage(page);
            return slot - (page * 9);
        }
    }
}
