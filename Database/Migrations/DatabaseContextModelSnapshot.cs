﻿// <auto-generated />
using System;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GhostfolioSidekick.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true);

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Comment")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("PlatformId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PlatformId");

                    b.ToTable("Accounts", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Balance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AccountId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("Balances", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Platform", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Platforms");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Activity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int?>("SortingPriority")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TransactionId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(34)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("Activities", (string)null);

                    b.HasDiscriminator<string>("Type").IsComplete(true).HasValue("Activity");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Market.MarketData", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Key", 0);

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT")
                        .HasColumnName("Date");

                    b.Property<string>("SymbolProfileDataSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileSymbol")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("TradingVolume")
                        .HasColumnType("TEXT")
                        .HasColumnName("TradingVolume");

                    b.HasKey("ID");

                    b.HasIndex("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.ToTable("MarketData", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Matches.ActivitySymbol", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("ActivityId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SymbolProfileDataSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileSymbol")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ActivityId");

                    b.HasIndex("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.ToTable("ActivitySymbol", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.CountryWeight", b =>
                {
                    b.Property<string>("Code")
                        .HasColumnType("TEXT");

                    b.Property<string>("Continent")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileDataSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileSymbol")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Weight")
                        .HasColumnType("TEXT");

                    b.HasKey("Code");

                    b.HasIndex("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.ToTable("CountryWeights", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.SectorWeight", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileDataSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("SymbolProfileSymbol")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Weight")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.ToTable("SectorWeights", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.SymbolProfile", b =>
                {
                    b.Property<string>("Symbol")
                        .HasColumnType("TEXT");

                    b.Property<string>("DataSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("AssetClass")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("AssetSubClass")
                        .HasColumnType("TEXT");

                    b.Property<string>("Comment")
                        .HasColumnType("TEXT");

                    b.Property<string>("ISIN")
                        .HasColumnType("TEXT");

                    b.Property<string>("Identifiers")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Symbol", "DataSource");

                    b.ToTable("SymbolProfiles", (string)null);
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.ActivityWithQuantityAndUnitPrice", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("PartialSymbolIdentifiers")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PartialSymbolIdentifiers");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("TEXT");

                    b.Property<string>("UnitPrice")
                        .HasColumnType("TEXT")
                        .HasColumnName("UnitPrice");

                    b.HasDiscriminator().HasValue("ActivityWithQuantityAndUnitPrice");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.CashDepositWithdrawalActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("Amount")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Amount");

                    b.HasDiscriminator().HasValue("CashDepositWithdrawalActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.DividendActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("Amount")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Amount");

                    b.Property<string>("Fees")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Fees");

                    b.Property<string>("PartialSymbolIdentifiers")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PartialSymbolIdentifiers");

                    b.Property<string>("Taxes")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Taxes");

                    b.HasDiscriminator().HasValue("DividendActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.FeeActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("Amount")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Amount");

                    b.HasDiscriminator().HasValue("FeeActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.InterestActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("Amount")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Amount");

                    b.HasDiscriminator().HasValue("InterestActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.KnownBalanceActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("Amount")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Amount");

                    b.HasDiscriminator().HasValue("KnownBalanceActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.LiabilityActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("PartialSymbolIdentifiers")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PartialSymbolIdentifiers");

                    b.Property<string>("Price")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Price");

                    b.HasDiscriminator().HasValue("LiabilityActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.RepayBondActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("PartialSymbolIdentifiers")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PartialSymbolIdentifiers");

                    b.Property<string>("TotalRepayAmount")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("TotalRepayAmount");

                    b.HasDiscriminator().HasValue("RepayBondActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.ValuableActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.Activity");

                    b.Property<string>("PartialSymbolIdentifiers")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PartialSymbolIdentifiers");

                    b.Property<string>("Price")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Price");

                    b.HasDiscriminator().HasValue("ValuableActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.BuySellActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.ActivityWithQuantityAndUnitPrice");

                    b.Property<string>("Fees")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Fees");

                    b.Property<string>("Taxes")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Taxes");

                    b.HasDiscriminator().HasValue("BuySellActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.GiftActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.ActivityWithQuantityAndUnitPrice");

                    b.HasDiscriminator().HasValue("GiftActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.SendAndReceiveActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.ActivityWithQuantityAndUnitPrice");

                    b.Property<string>("Fees")
                        .IsRequired()
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("Fees");

                    b.HasDiscriminator().HasValue("SendAndReceiveActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Types.StakingRewardActivity", b =>
                {
                    b.HasBaseType("GhostfolioSidekick.Model.Activities.ActivityWithQuantityAndUnitPrice");

                    b.HasDiscriminator().HasValue("StakingRewardActivity");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Account", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Accounts.Platform", "Platform")
                        .WithMany()
                        .HasForeignKey("PlatformId");

                    b.Navigation("Platform");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Balance", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Accounts.Account", null)
                        .WithMany("Balance")
                        .HasForeignKey("AccountId");

                    b.OwnsOne("GhostfolioSidekick.Model.Money", "Money", b1 =>
                        {
                            b1.Property<int>("BalanceId")
                                .HasColumnType("INTEGER");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("TEXT")
                                .HasColumnName("Amount");

                            b1.HasKey("BalanceId");

                            b1.ToTable("Balances");

                            b1.WithOwner()
                                .HasForeignKey("BalanceId");

                            b1.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b2 =>
                                {
                                    b2.Property<int>("MoneyBalanceId")
                                        .HasColumnType("INTEGER");

                                    b2.Property<string>("Symbol")
                                        .IsRequired()
                                        .HasColumnType("TEXT")
                                        .HasColumnName("Currency");

                                    b2.HasKey("MoneyBalanceId");

                                    b2.ToTable("Balances");

                                    b2.WithOwner()
                                        .HasForeignKey("MoneyBalanceId");
                                });

                            b1.Navigation("Currency")
                                .IsRequired();
                        });

                    b.Navigation("Money")
                        .IsRequired();
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Activities.Activity", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Accounts.Account", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Market.MarketData", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Symbols.SymbolProfile", null)
                        .WithMany("MarketData")
                        .HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.OwnsOne("GhostfolioSidekick.Model.Money", "Close", b1 =>
                        {
                            b1.Property<int>("MarketDataID")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("TEXT")
                                .HasColumnName("Close");

                            b1.HasKey("MarketDataID");

                            b1.ToTable("MarketData");

                            b1.WithOwner()
                                .HasForeignKey("MarketDataID");

                            b1.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b2 =>
                                {
                                    b2.Property<int>("MoneyMarketDataID")
                                        .HasColumnType("integer");

                                    b2.Property<string>("Symbol")
                                        .IsRequired()
                                        .HasColumnType("TEXT")
                                        .HasColumnName("CurrencyClose");

                                    b2.HasKey("MoneyMarketDataID");

                                    b2.ToTable("MarketData");

                                    b2.WithOwner()
                                        .HasForeignKey("MoneyMarketDataID");
                                });

                            b1.Navigation("Currency")
                                .IsRequired();
                        });

                    b.OwnsOne("GhostfolioSidekick.Model.Money", "High", b1 =>
                        {
                            b1.Property<int>("MarketDataID")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("TEXT")
                                .HasColumnName("High");

                            b1.HasKey("MarketDataID");

                            b1.ToTable("MarketData");

                            b1.WithOwner()
                                .HasForeignKey("MarketDataID");

                            b1.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b2 =>
                                {
                                    b2.Property<int>("MoneyMarketDataID")
                                        .HasColumnType("integer");

                                    b2.Property<string>("Symbol")
                                        .IsRequired()
                                        .HasColumnType("TEXT")
                                        .HasColumnName("CurrencyHigh");

                                    b2.HasKey("MoneyMarketDataID");

                                    b2.ToTable("MarketData");

                                    b2.WithOwner()
                                        .HasForeignKey("MoneyMarketDataID");
                                });

                            b1.Navigation("Currency")
                                .IsRequired();
                        });

                    b.OwnsOne("GhostfolioSidekick.Model.Money", "Low", b1 =>
                        {
                            b1.Property<int>("MarketDataID")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("TEXT")
                                .HasColumnName("Low");

                            b1.HasKey("MarketDataID");

                            b1.ToTable("MarketData");

                            b1.WithOwner()
                                .HasForeignKey("MarketDataID");

                            b1.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b2 =>
                                {
                                    b2.Property<int>("MoneyMarketDataID")
                                        .HasColumnType("integer");

                                    b2.Property<string>("Symbol")
                                        .IsRequired()
                                        .HasColumnType("TEXT")
                                        .HasColumnName("CurrencyLow");

                                    b2.HasKey("MoneyMarketDataID");

                                    b2.ToTable("MarketData");

                                    b2.WithOwner()
                                        .HasForeignKey("MoneyMarketDataID");
                                });

                            b1.Navigation("Currency")
                                .IsRequired();
                        });

                    b.OwnsOne("GhostfolioSidekick.Model.Money", "Open", b1 =>
                        {
                            b1.Property<int>("MarketDataID")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Amount")
                                .HasColumnType("TEXT")
                                .HasColumnName("Open");

                            b1.HasKey("MarketDataID");

                            b1.ToTable("MarketData");

                            b1.WithOwner()
                                .HasForeignKey("MarketDataID");

                            b1.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b2 =>
                                {
                                    b2.Property<int>("MoneyMarketDataID")
                                        .HasColumnType("integer");

                                    b2.Property<string>("Symbol")
                                        .IsRequired()
                                        .HasColumnType("TEXT")
                                        .HasColumnName("CurrencyOpen");

                                    b2.HasKey("MoneyMarketDataID");

                                    b2.ToTable("MarketData");

                                    b2.WithOwner()
                                        .HasForeignKey("MoneyMarketDataID");
                                });

                            b1.Navigation("Currency")
                                .IsRequired();
                        });

                    b.Navigation("Close")
                        .IsRequired();

                    b.Navigation("High")
                        .IsRequired();

                    b.Navigation("Low")
                        .IsRequired();

                    b.Navigation("Open")
                        .IsRequired();
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Matches.ActivitySymbol", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Activities.Activity", "Activity")
                        .WithMany()
                        .HasForeignKey("ActivityId");

                    b.HasOne("GhostfolioSidekick.Model.Symbols.SymbolProfile", "SymbolProfile")
                        .WithMany()
                        .HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource");

                    b.Navigation("Activity");

                    b.Navigation("SymbolProfile");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.CountryWeight", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Symbols.SymbolProfile", null)
                        .WithMany("CountryWeight")
                        .HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.SectorWeight", b =>
                {
                    b.HasOne("GhostfolioSidekick.Model.Symbols.SymbolProfile", null)
                        .WithMany("SectorWeights")
                        .HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.SymbolProfile", b =>
                {
                    b.OwnsOne("GhostfolioSidekick.Model.Currency", "Currency", b1 =>
                        {
                            b1.Property<string>("SymbolProfileSymbol")
                                .HasColumnType("TEXT");

                            b1.Property<string>("SymbolProfileDataSource")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Symbol")
                                .IsRequired()
                                .HasColumnType("TEXT")
                                .HasColumnName("Currency");

                            b1.HasKey("SymbolProfileSymbol", "SymbolProfileDataSource");

                            b1.ToTable("SymbolProfiles");

                            b1.WithOwner()
                                .HasForeignKey("SymbolProfileSymbol", "SymbolProfileDataSource");
                        });

                    b.Navigation("Currency")
                        .IsRequired();
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Accounts.Account", b =>
                {
                    b.Navigation("Balance");
                });

            modelBuilder.Entity("GhostfolioSidekick.Model.Symbols.SymbolProfile", b =>
                {
                    b.Navigation("CountryWeight");

                    b.Navigation("MarketData");

                    b.Navigation("SectorWeights");
                });
#pragma warning restore 612, 618
        }
    }
}
