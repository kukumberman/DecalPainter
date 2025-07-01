using System.Collections.Generic;
using NUnit.Framework;

public class CollectionDiffCheckerTests
{
    [Test]
    public void ItWorks()
    {
        var EMPTY = new int[0];

        var diff = new CollectionDiffChecker<int>();

        diff.Execute(1, 2);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, diff.Added);
        CollectionAssert.AreEquivalent(EMPTY, diff.Removed);

        diff.Execute(1, 2);
        Assert.AreEqual(0, diff.Added.Count);
        Assert.AreEqual(0, diff.Removed.Count);

        diff.Execute(1, 2, 3);
        CollectionAssert.AreEquivalent(new[] { 3 }, diff.Added);
        CollectionAssert.AreEquivalent(EMPTY, diff.Removed);

        diff.Execute(2, 3, 4);
        CollectionAssert.AreEquivalent(new[] { 4 }, diff.Added);
        CollectionAssert.AreEquivalent(new[] { 1 }, diff.Removed);

        diff.Execute(new List<int>());
        CollectionAssert.AreEquivalent(EMPTY, diff.Added);
        CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, diff.Removed);
    }
}
